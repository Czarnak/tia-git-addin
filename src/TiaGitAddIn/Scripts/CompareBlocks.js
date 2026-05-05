const fs = require('fs');
const path = require('path');

/**
 * CompareBlocks.js
 * 
 * Node.js bridge for Siemens TIA Portal Block Comparison.
 * This script leverages Siemens internal libraries to perform a deep semantic comparison
 * and outputs the full result tree as JSON.
 * 
 * Usage: node CompareBlocks.js <leftXmlPath> <rightXmlPath>
 */

// Siemens Models & Adapters
const { smlToCodeBlockAdapter } = require('@web-engr/sml-to-codeblock-adapter');
const { CodeBlockCompareConfiguration, CodeBlockComparer } = require('@compare-engineering/codeblock-comparer');
const { PairedCompareState } = require('@web-engr/codeblock-model');

const args = process.argv.slice(2);

if (args.length < 2) {
    console.error("Usage: node CompareBlocks.js <leftXml> <rightXml>");
    process.exit(1);
}

const leftPath = args[0];
const rightPath = args[1];

try {
    if (!fs.existsSync(leftPath)) throw new Error(`Left file not found: ${leftPath}`);
    if (!fs.existsSync(rightPath)) throw new Error(`Right file not found: ${rightPath}`);

    const leftXml = fs.readFileSync(leftPath, 'utf8');
    const rightXml = fs.readFileSync(rightPath, 'utf8');

    // 1. Convert SML (XML) to CodeBlock Model
    const leftModel = smlToCodeBlockAdapter.convert(leftXml);
    const rightModel = smlToCodeBlockAdapter.convert(rightXml);

    // 2. Configure and execute comparison
    const config = new CodeBlockCompareConfiguration();
    const comparer = new CodeBlockComparer(config);
    const compareResults = comparer.Compare(leftModel, rightModel);

    // 3. Serialize to the format expected by TiaGitAddIn.Services.SactJsonParser
    const output = {
        Left: leftPath,
        Right: rightPath,
        State: mapState(compareResults.state),
        Interface: serializeInterface(compareResults.properties.get("Interface")),
        Content: serializeContent(compareResults.properties.get("Content")),
        Attributes: serializeAttributes(compareResults.properties.get("Attributes"))
    };

    // Output JSON to StdOut
    console.log(JSON.stringify(output));

} catch (e) {
    console.error(e.stack || e.message);
    process.exit(1);
}

/**
 * Maps Siemens internal PairedCompareState to the string expected by SactJsonParser.
 */
function mapState(state) {
    if (state === undefined || state === null) return "Equal";
    
    // Attempt robust mapping using library enum if available
    // PairedCompareState values: 0=Equal, 1=Changed/Different, 2=MissingOnLeft, 3=MissingOnRight
    if (typeof state === 'number') {
        if (state === PairedCompareState.Changed || state === PairedCompareState.Different) return "Changed";
        if (state === PairedCompareState.MissingOnLeft) return "MissingOnLeft";
        if (state === PairedCompareState.MissingOnRight) return "MissingOnRight";
        if (state === PairedCompareState.Equal) return "Equal";
    }

    // Fallback to string matching
    const s = state.toString();
    if (s.includes("Changed") || s.includes("Different")) return "Changed";
    if (s.includes("MissingOnLeft")) return "MissingOnLeft";
    if (s.includes("MissingOnRight")) return "MissingOnRight";
    if (s.includes("Equal")) return "Equal";
    
    return s;
}

/**
 * Helper to get the value from a PropertyResult.
 */
function mapValue(res) {
    if (!res) return null;
    return res.right !== undefined ? res.right : res.left;
}

/**
 * Serializes the Interface comparison result.
 */
function serializeInterface(res) {
    if (!res) return null;

    const sections = {};
    const sectionsCollection = res.collection ? res.collection.get("Sections") : null;
    if (sectionsCollection && sectionsCollection.pairedEntries) {
        for (const [name, sectionRes] of sectionsCollection.pairedEntries) {
            sections[name] = {
                State: mapState(sectionRes.state)
            };
        }
    }

    return {
        State: mapState(res.state),
        Sections: sections
    };
}

/**
 * Serializes the Content (Networks) comparison result.
 */
function serializeContent(res) {
    if (!res) return null;
    
    const networks = {};
    // Networks are usually in a collection named 'Networks'
    const networksCollection = res.collection ? res.collection.get("Networks") : null;
    
    if (networksCollection && networksCollection.pairedEntries) {
        for (const [uId, netRes] of networksCollection.pairedEntries) {
            networks[uId] = {
                State: mapState(netRes.state),
                Number: {
                    Left: netRes.left ? netRes.left.Number : null,
                    Right: netRes.right ? netRes.right.Number : null
                },
                Title: mapValue(netRes.properties.get("Title")),
                Comment: mapValue(netRes.properties.get("Comment")),
                Body: serializeBody(netRes.properties.get("Body"))
            };
        }
    }

    return {
        State: mapState(res.state),
        Networks: networks
    };
}

/**
 * Serializes the Body (Components/Instructions) of a network.
 */
function serializeBody(res) {
    if (!res) return {};
    
    const body = {};
    // Components are in a collection named 'Components'
    const componentsCollection = res.collection ? res.collection.get("Components") : null;
    
    if (componentsCollection && componentsCollection.pairedEntries) {
        for (const [uId, compRes] of componentsCollection.pairedEntries) {
            // Use the available model (right for new/changed, left for deleted)
            const model = compRes.right || compRes.left;
            if (!model) continue;

            body[uId] = {
                name: model.Name || model.name || "",
                uId: uId,
                isStartElement: model.IsStartElement !== undefined ? model.IsStartElement : model.isStartElement,
                negated: model.Negated !== undefined ? model.Negated : model.negated,
                DisplayName: model.DisplayName,
                TemplateType: model.TemplateType,
                TopOperandConnector: model.TopOperandConnector ? { 
                    DisplayName: model.TopOperandConnector.DisplayName 
                } : null,
                outputConnectors: serializeConnectors(model.OutputConnectors || model.outputConnectors),
                inputConnectors: serializeConnectors(model.InputConnectors || model.inputConnectors)
            };
        }
    }
    
    return body;
}

/**
 * Serializes list of connectors for a component.
 */
function serializeConnectors(connectors) {
    if (!connectors) return [];
    
    // Connectors might be an array or a collection
    const list = Array.isArray(connectors) ? connectors : (connectors.toArray ? connectors.toArray() : []);
    
    return list.map(c => ({
        uId: c.UId || c.uId,
        PartnerUId: c.PartnerUId || c.partnerUId
    }));
}

/**
 * Serializes block attributes.
 */
function serializeAttributes(res) {
    if (!res) return {};
    const attributes = {};
    if (res.properties) {
        for (const [name, propRes] of res.properties) {
            attributes[name] = mapValue(propRes);
        }
    }
    return attributes;
}

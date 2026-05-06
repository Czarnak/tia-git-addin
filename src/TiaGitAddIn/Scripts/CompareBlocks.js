const fs = require('fs');
const path = require('path');

/**
 * CompareBlocks.js
 * 
 * Node.js bridge for Siemens TIA Portal Block Comparison.
 * This script leverages Siemens internal libraries to perform a deep semantic comparison
 * and outputs the full result tree as JSON.
 * 
 * Usage: node CompareBlocks.js <leftXmlPath> <rightXmlPath> [nodeModulesPath]
 */

const args = process.argv.slice(2);

if (args.length < 2) {
    console.error("Usage: node CompareBlocks.js <leftXml> <rightXml> [nodeModulesPath]");
    process.exit(1);
}

const leftPath = args[0];
const rightPath = args[1];

let SimaticMLParser, SmlToCodeBlockAdapter, CodeBlockCompareConfiguration, CodeBlockComparer, PairedCompareState;
let NetworkIndexMatchStrategy, InterfaceIndexMatchStrategy;

if (args.length >= 3) {
    const nodeModulesPath = args[2];
    module.paths.push(nodeModulesPath);
    
    try {
        SimaticMLParser = require('@web-engr/simaticml-model').SimaticMLParser;
        SmlToCodeBlockAdapter = require('@web-engr/sml-to-codeblock-adapter').SmlToCodeBlockAdapter;
        const comparerMod = require('@compare-engineering/codeblock-comparer');
        CodeBlockCompareConfiguration = comparerMod.CodeBlockCompareConfiguration;
        CodeBlockComparer = comparerMod.CodeBlockComparer;
        NetworkIndexMatchStrategy = comparerMod.IndexMatchStrategy;
        PairedCompareState = require('@compare-engineering/compare-state').PairedCompareState;
        InterfaceIndexMatchStrategy = require('@compare-engineering/blockinterface-comparer').IndexMatchStrategy;
    } catch (e) {
        SimaticMLParser = require(path.join(nodeModulesPath, '@web-engr', 'simaticml-model')).SimaticMLParser;
        SmlToCodeBlockAdapter = require(path.join(nodeModulesPath, '@web-engr', 'sml-to-codeblock-adapter')).SmlToCodeBlockAdapter;
        const comparerMod = require(path.join(nodeModulesPath, '@compare-engineering', 'codeblock-comparer'));
        CodeBlockCompareConfiguration = comparerMod.CodeBlockCompareConfiguration;
        CodeBlockComparer = comparerMod.CodeBlockComparer;
        NetworkIndexMatchStrategy = comparerMod.IndexMatchStrategy;
        PairedCompareState = require(path.join(nodeModulesPath, '@compare-engineering', 'compare-state')).PairedCompareState;
        InterfaceIndexMatchStrategy = require(path.join(nodeModulesPath, '@compare-engineering', 'blockinterface-comparer')).IndexMatchStrategy;
    }
} else {
    SimaticMLParser = require('@web-engr/simaticml-model').SimaticMLParser;
    SmlToCodeBlockAdapter = require('@web-engr/sml-to-codeblock-adapter').SmlToCodeBlockAdapter;
    const comparerMod = require('@compare-engineering/codeblock-comparer');
    CodeBlockCompareConfiguration = comparerMod.CodeBlockCompareConfiguration;
    CodeBlockComparer = comparerMod.CodeBlockComparer;
    NetworkIndexMatchStrategy = comparerMod.IndexMatchStrategy;
    PairedCompareState = require('@compare-engineering/compare-state').PairedCompareState;
    InterfaceIndexMatchStrategy = require('@compare-engineering/blockinterface-comparer').IndexMatchStrategy;
}

try {
    if (!fs.existsSync(leftPath)) throw new Error(`Left file not found: ${leftPath}`);
    if (!fs.existsSync(rightPath)) throw new Error(`Right file not found: ${rightPath}`);

    const leftXml = fs.readFileSync(leftPath, 'utf8');
    const rightXml = fs.readFileSync(rightPath, 'utf8');

    // 1. Parse SimaticML (XML)
    const leftSml = SimaticMLParser.tryParse(leftXml);
    const rightSml = SimaticMLParser.tryParse(rightXml);
    
    if (!leftSml) throw new Error("Failed to parse left XML as SimaticML");
    if (!rightSml) throw new Error("Failed to parse right XML as SimaticML");

    // 2. Convert SML to CodeBlock Model
    const leftModel = SmlToCodeBlockAdapter.tryBuild(leftSml);
    const rightModel = SmlToCodeBlockAdapter.tryBuild(rightSml);
    
    if (!leftModel) throw new Error("Failed to convert left SimaticML to CodeBlock Model");
    if (!rightModel) throw new Error("Failed to convert right SimaticML to CodeBlock Model");

    // 3. Configure and execute comparison
    const config = new CodeBlockCompareConfiguration();
    config.NetworkMatchStrategy = new NetworkIndexMatchStrategy();
    config.InterfaceMatchStrategy = new InterfaceIndexMatchStrategy();
    
    const comparer = new CodeBlockComparer(config);
    const comparison = comparer.Compare(leftModel, rightModel);

    // 4. Serialize result using the comparison wrapper
    const output = {
        Left: leftPath,
        Right: rightPath,
        State: mapState(comparison.right.CompareState),
        Interface: serializeInterface(comparison.right.BlockInterface),
        Content: serializeContent(comparison.right.Content),
        Attributes: serializeAttributes(comparison.right.Properties)
    };

    console.log(JSON.stringify(output));

} catch (e) {
    console.error(e.stack || e.message);
    process.exit(1);
}

/**
 * Maps Siemens internal CompareState/PairedCompareState to the string expected by SactJsonParser.
 */
function mapState(state) {
    if (state === undefined || state === null) return "Equal";
    
    // Check if it's a number (from enum)
    if (typeof state === 'number') {
        if (state === PairedCompareState.Different || state === 3) return "Changed";
        if (state === PairedCompareState.MissingOnLeft || state === 1) return "MissingOnLeft";
        if (state === PairedCompareState.MissingOnRight || state === 2) return "MissingOnRight";
        return "Equal";
    }

    // Fallback to string matching
    const s = state.toString();
    if (s.includes("Changed") || s.includes("Different")) return "Changed";
    if (s.includes("MissingOnLeft")) return "MissingOnLeft";
    if (s.includes("MissingOnRight")) return "MissingOnRight";
    
    return "Equal";
}

/**
 * Helper to get the value from a Property.
 */
function mapValue(prop) {
    return prop ? prop.Value : null;
}

/**
 * Serializes the Interface comparison result.
 */
function serializeInterface(res) {
    if (!res) return null;

    const sections = {};
    if (res.Sections) {
        res.Sections.forEach(section => {
            sections[section.Name] = {
                State: mapState(section.CompareState)
            };
        });
    }

    return {
        State: mapState(res.CompareState),
        Sections: sections
    };
}

/**
 * Serializes the Content (Networks) comparison result.
 */
function serializeContent(res) {
    if (!res) return null;
    
    const networks = {};
    if (res.Networks) {
        res.Networks.forEach((net, index) => {
            const uId = net.UId || `net_${index}`;
            networks[uId] = {
                State: mapState(net.CompareState),
                Number: {
                    Left: net.Number, // SACT internal models often share same fields after comparison
                    Right: net.Number
                },
                Title: net.Title,
                Comment: net.Comment,
                Body: serializeBody(net.Content ? net.Content.Body : null)
            };
        });
    }

    return {
        State: mapState(res.CompareState),
        Networks: networks
    };
}

/**
 * Serializes the Body (Components/Instructions) of a network.
 */
function serializeBody(body) {
    if (!body || !body.Components) return {};
    
    const serializedBody = {};
    body.Components.forEach(comp => {
        const uId = comp.UId || comp.uId;
        serializedBody[uId] = {
            name: comp.Name || "",
            uId: uId,
            isStartElement: comp.IsStartElement,
            negated: comp.Negated,
            DisplayName: comp.DisplayName,
            TemplateType: comp.TemplateType,
            TopOperandConnector: comp.TopOperandConnector ? { 
                DisplayName: comp.TopOperandConnector.DisplayName 
            } : null,
            outputConnectors: serializeConnectors(comp.OutputConnectors),
            inputConnectors: serializeConnectors(comp.InputConnectors)
        };
    });
    
    return serializedBody;
}

/**
 * Serializes list of connectors for a component.
 */
function serializeConnectors(connectors) {
    if (!connectors) return [];
    
    const list = Array.isArray(connectors) ? connectors : (connectors.toArray ? connectors.toArray() : []);
    
    return list.map(c => ({
        uId: c.UId || c.uId,
        PartnerUId: c.PartnerUId || c.partnerUId
    }));
}

/**
 * Serializes block attributes.
 */
function serializeAttributes(properties) {
    if (!properties) return {};
    const attributes = {};
    properties.forEach(prop => {
        if (prop.Name) {
            attributes[prop.Name] = prop.Value;
        }
    });
    return attributes;
}

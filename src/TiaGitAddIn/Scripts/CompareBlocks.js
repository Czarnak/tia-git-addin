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

    // Scrape XML for UId -> Name/Type/Powerrail mappings as fallback
    const leftXmlInfo = scrapeXmlInfo(leftXml);
    const rightXmlInfo = scrapeXmlInfo(rightXml);

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
    const debugData = {
        firstComponentKeys: [],
        introspection: {}
    };

    const output = {
        Left: leftPath,
        Right: rightPath,
        State: mapState(comparison.right.CompareState),
        Interface: serializeInterface(comparison.right.BlockInterface),
        Content: serializeContent(comparison.right.Content, rightXmlInfo, debugData),
        Attributes: serializeAttributes(comparison.right.Properties),
        DebugInfo: debugData
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
 * Serializes the Interface comparison result.
 */
function serializeInterface(res) {
    if (!res) return null;

    const sections = {};
    const col = getSafeCollection(res.Sections);
    col.forEach(section => {
        const name = getProperty(section, ['Name', 'name']);
        if (name) {
            sections[name] = {
                State: mapState(getProperty(section, ['CompareState', 'compareState']))
            };
        }
    });

    return {
        State: mapState(res.CompareState),
        Sections: sections
    };
}

/**
 * Serializes the Content (Networks) comparison result.
 */
function serializeContent(res, xmlInfo, debugData) {
    if (!res) return null;
    
    const networks = {};
    const nets = getSafeCollection(res.Networks);
    
    nets.forEach((net, index) => {
        if (index === 0 && debugData) {
            debugData.introspection.networkKeys = safeGetKeys(net);
            const content = getProperty(net, ['Content', 'content', 'm_Content']);
            if (content) {
                debugData.introspection.contentKeys = safeGetKeys(content);
                const body = getProperty(content, ['Body', 'body', 'm_Body']);
                if (body) {
                    debugData.introspection.bodyKeys = safeGetKeys(body);
                }
            }
        }

        const uId = getProperty(net, ['UId', 'uId']) || `net_${index}`;
        networks[uId] = {
            State: mapState(getProperty(net, ['CompareState', 'compareState'])),
            Number: {
                Left: getProperty(net, ['Number', 'number', 'm_Number']),
                Right: getProperty(net, ['Number', 'number', 'm_Number'])
            },
            Title: getProperty(net, ['Title', 'title']),
            Comment: getProperty(net, ['Comment', 'comment']),
            Body: serializeBody(getProperty(net, ['Content', 'content', 'm_Content']), xmlInfo, debugData)
        };
    });

    return {
        State: mapState(res.CompareState),
        Networks: networks
    };
}

/**
 * Helper to handle various Siemens collection types.
 */
function getSafeCollection(col) {
    if (!col) return [];
    if (Array.isArray(col)) return col;
    if (typeof col.toArray === 'function') return col.toArray();
    if (typeof col.ToList === 'function') return col.ToList().ToArray();
    
    const list = [];
    const count = col.Count !== undefined ? col.Count : (col.count !== undefined ? col.count : (col.m_Count !== undefined ? col.m_Count : undefined));
    
    if (typeof count === 'number') {
        const itemFunc = col.Item || col.item || col.get_Item || col.get_item;
        for (let i = 0; i < count; i++) {
            try {
                list.push(typeof itemFunc === 'function' ? itemFunc.call(col, i) : col[i]);
            } catch (e) {}
        }
    }
    return list;
}

/**
 * Helper to try multiple property names.
 */
function getProperty(obj, names) {
    if (!obj) return undefined;
    for (const name of names) {
        if (obj[name] !== undefined) {
            const val = obj[name];
            if (typeof val === 'string' && val === "" && names.indexOf(name) < names.length - 1) continue;
            return val;
        }
        // Try m_ prefix (private fields often exposed)
        const mPrefixed = "m_" + name;
        if (obj[mPrefixed] !== undefined) return obj[mPrefixed];
        
        // Try camelCase and PascalCase variations
        const pascal = name.charAt(0).toUpperCase() + name.slice(1);
        if (obj[pascal] !== undefined) return obj[pascal];
        const camel = name.charAt(0).toLowerCase() + name.slice(1);
        if (obj[camel] !== undefined) return obj[camel];
    }
    return undefined;
}

function safeGetKeys(obj) {
    try {
        const keys = Object.keys(obj);
        if (keys.length > 0) return keys;
        const k2 = [];
        for (let k in obj) k2.push(k);
        return k2;
    } catch (e) {
        return ["error: " + e.message];
    }
}

/**
 * Serializes the Body (Components/Instructions) of a network.
 */
function serializeBody(content, xmlInfo, debugData) {
    if (!content) return {};
    const body = getProperty(content, ['Body', 'body', 'm_Body']);
    if (!body) return {};

    const componentsCol = getProperty(body, ['Components', 'components', 'm_Components', 'Parts', 'parts', 'Instructions', 'instructions']);
    if (!componentsCol) return {};

    const serializedBody = {};
    const components = getSafeCollection(componentsCol);

    components.forEach((comp, index) => {
        if (index === 0 && debugData && debugData.firstComponentKeys.length === 0) {
            debugData.firstComponentKeys = safeGetKeys(comp);
        }

        const uId = getProperty(comp, ['UId', 'uId', 'm_UId']);
        const displayName = getProperty(comp, ['DisplayName', 'displayName', 'm_DisplayName']);
        
        // Try to get name from object, fallback to XML info, then fallback to mapping DisplayName
        let name = getProperty(comp, ['TemplateName', 'TypeName', 'InstructionName', 'Name', 'name']);
        if (!name && xmlInfo[uId]) {
            name = mapSimaticPartToSactName(xmlInfo[uId].name);
        }
        if (!name && displayName) {
            name = mapSimaticPartToSactName(displayName);
        }

        let isStart = getProperty(comp, ['IsStartElement', 'isStartElement', 'm_StartElement', 'IsPowerrail', 'isPowerrail']);
        if (isStart === undefined && xmlInfo[uId]) {
            isStart = xmlInfo[uId].isPowerrail;
        }

        serializedBody[uId] = {
            name: name || "",
            uId: uId,
            isStartElement: isStart,
            negated: getProperty(comp, ['Negated', 'negated', 'm_Negated']),
            DisplayName: displayName,
            TemplateType: getProperty(comp, ['TemplateType', 'templateType']),
            TopOperandConnector: getProperty(comp, ['TopOperandConnector', 'topOperandConnector', 'm_TopOperandConnector']) ? { 
                DisplayName: getProperty(getProperty(comp, ['TopOperandConnector', 'topOperandConnector', 'm_TopOperandConnector']), ['DisplayName', 'displayName'])
            } : null,
            outputConnectors: serializeConnectors(getProperty(comp, ['OutputConnectors', 'outputConnectors', 'm_OutputConnectors', 'Pins', 'pins'])),
            inputConnectors: serializeConnectors(getProperty(comp, ['InputConnectors', 'inputConnectors', 'm_InputConnectors', 'Pins', 'pins']))
        };
    });
    
    return serializedBody;
}

/**
 * Serializes list of connectors for a component.
 */
function serializeConnectors(connectors) {
    const list = getSafeCollection(connectors);
    return list.map(c => ({
        uId: getProperty(c, ['UId', 'uId', 'm_UId']),
        PartnerUId: getProperty(c, ['PartnerUId', 'partnerUId', 'PartnerPinUId', 'partnerPinUId', 'm_PartnerUId'])
    }));
}

/**
 * Serializes block attributes.
 */
function serializeAttributes(properties) {
    if (!properties) return {};
    const attributes = {};
    const props = getSafeCollection(properties);
    props.forEach(prop => {
        const name = getProperty(prop, ['Name', 'name']);
        const value = getProperty(prop, ['Value', 'value']);
        if (name) {
            attributes[name] = value;
        }
    });
    return attributes;
}

/**
 * Simple SimaticML scraper to extract UId mappings.
 */
function scrapeXmlInfo(xml) {
    const info = {};
    // Extract Parts
    const partRegex = /<Part\s+Name="([^"]+)"\s+UId="(\d+)"/g;
    let m;
    while (m = partRegex.exec(xml)) {
        info[m[2]] = { name: m[1], isPart: true };
    }
    // Extract Wires to find Powerrail
    const wireRegex = /<Wire\s+UId="(\d+)">([\s\S]*?)<\/Wire>/g;
    while (m = wireRegex.exec(xml)) {
        const uid = m[1];
        const content = m[2];
        if (content.includes("<Powerrail/>")) {
            info[uid] = { name: "BranchWireData", isPowerrail: true };
        }
    }
    return info;
}

/**
 * Maps SimaticML Part Name to SACT Internal Name.
 */
function mapSimaticPartToSactName(simaticName) {
    if (!simaticName) return "";
    switch (simaticName) {
        case "Contact": return "LadContactData";
        case "Coil": return "LadCoilData";
        case "BranchWire":
        case "BranchWireData": return "BranchWireData";
        case "Or": return "LadOrWireData";
        case "Box": return "LadBoxData";
        default: return simaticName;
    }
}

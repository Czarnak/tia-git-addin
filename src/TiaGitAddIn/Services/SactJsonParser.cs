using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using TiaGitAddIn.Models.Sact;

namespace TiaGitAddIn.Services
{
    public static class SactJsonParser
    {
        public static SactCompareResult? ParseCompareResult(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            // Strip BOM and invisible characters
            json = json.Trim();
            json = json.TrimStart('\uFEFF', '\u200B');
            while (json.StartsWith("\xEF\xBB\xBF"))
            {
                json = json.Substring(3).Trim();
            }

            try
            {
                var dict = JObjectToDictionary(JObject.Parse(json));

                if (dict == null)
                    return null;

                SactCompareResult result = new();

                if (dict.TryGetValue("Left", out object? leftObj) && leftObj is string leftStr)
                    result.Left = leftStr;
                if (dict.TryGetValue("Right", out object? rightObj) && rightObj is string rightStr)
                    result.Right = rightStr;
                if (dict.TryGetValue("State", out object? stateObj) && stateObj is string stateStr)
                    result.State = ParseCompareState(stateStr);

                if (dict.TryGetValue("Interface", out object? interfaceObj) && interfaceObj is Dictionary<string, object> interfaceDict)
                {
                    result.Interface = new SactInterfaceResult
                    {
                        State = GetState(interfaceDict),
                        Sections = GetDict(interfaceDict, "Sections")
                    };
                }

                if (dict.TryGetValue("Content", out object? contentObj) && contentObj is Dictionary<string, object> contentDict)
                {
                    result.Content = new SactContentResult
                    {
                        State = GetState(contentDict)
                    };

                    if (contentDict.TryGetValue("Networks", out object? networksObj) && networksObj is Dictionary<string, object> networksDict)
                    {
                        foreach (var kvp in networksDict)
                        {
                            if (kvp.Value is Dictionary<string, object> networkDict)
                            {
                                result.Content.Networks[kvp.Key] = ParseNetwork(networkDict);
                            }
                        }
                    }
                }

                if (dict.TryGetValue("Attributes", out object? attributesObj) && attributesObj is Dictionary<string, object> attributesDict)
                {
                    result.Attributes = attributesDict;
                }

                return result;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private static Dictionary<string, object> JObjectToDictionary(JObject jobj)
        {
            Dictionary<string, object> dict = new();
            foreach (var prop in jobj.Properties())
                dict[prop.Name] = JTokenToObject(prop.Value);
            return dict;
        }

        private static object JTokenToObject(JToken token)
        {
            switch (token.Type)
            {
                case JTokenType.Object: return JObjectToDictionary((JObject)token);
                case JTokenType.Array: return ((JArray)token).Select(JTokenToObject).ToArray();
                case JTokenType.Boolean: return (bool)token;
                case JTokenType.Integer: return (long)token;
                case JTokenType.Float: return (double)token;
                default: return (string?)token ?? string.Empty;
            }
        }

        private static SactNetworkResult ParseNetwork(Dictionary<string, object> dict)
        {
            SactNetworkResult network = new()
            {
                State = GetState(dict)
            };

            if (dict.TryGetValue("Title", out object? titleObj) && titleObj is string titleStr)
                network.Title = titleStr;
            if (dict.TryGetValue("Comment", out object? commentObj) && commentObj is string commentStr)
                network.Comment = commentStr;

            if (dict.TryGetValue("Number", out object? numberObj) && numberObj is Dictionary<string, object> numberDict)
            {
                if (numberDict.TryGetValue("Left", out object? leftNum) && leftNum is IConvertible leftConv)
                    network.Number.Left = leftConv.ToInt32(null);
                if (numberDict.TryGetValue("Right", out object? rightNum) && rightNum is IConvertible rightConv)
                    network.Number.Right = rightConv.ToInt32(null);
            }

            if (dict.TryGetValue("Body", out object? bodyObj) && bodyObj is Dictionary<string, object> bodyDict)
            {
                foreach (var kvp in bodyDict)
                {
                    if (kvp.Value is Dictionary<string, object> compDict)
                    {
                        network.Body[kvp.Key] = ParseComponent(compDict);
                    }
                }
            }

            return network;
        }

        private static SactComponentData ParseComponent(Dictionary<string, object> dict)
        {
            SactComponentData comp = new();

            if (dict.TryGetValue("name", out object? nameObj) && nameObj is string nameStr)
                comp.name = nameStr;
            if (dict.TryGetValue("uId", out object? uidObj) && uidObj is string uidStr)
                comp.uId = uidStr;

            if (dict.TryGetValue("isStartElement", out object? startObj) && startObj is bool startBool)
                comp.isStartElement = startBool;
            if (dict.TryGetValue("negated", out object? negatedObj) && negatedObj is bool negatedBool)
                comp.negated = negatedBool;

            if (dict.TryGetValue("DisplayName", out object? displayObj) && displayObj is string displayStr)
                comp.DisplayName = displayStr;
            if (dict.TryGetValue("TemplateType", out object? typeObj) && typeObj is string typeStr)
                comp.TemplateType = typeStr;

            if (dict.TryGetValue("TopOperandConnector", out object? topObj) && topObj is Dictionary<string, object> topDict)
            {
                comp.TopOperandConnector = new SactOperandConnector();
                if (topDict.TryGetValue("DisplayName", out object? topDisp) && topDisp is string topDispStr)
                    comp.TopOperandConnector.DisplayName = topDispStr;
            }

            if (dict.TryGetValue("outputConnectors", out object? outObj) && outObj is System.Collections.IEnumerable outEnum)
            {
                foreach (var item in outEnum.OfType<Dictionary<string, object>>())
                {
                    comp.outputConnectors.Add(ParseConnector(item));
                }
            }

            if (dict.TryGetValue("inputConnectors", out object? inObj) && inObj is System.Collections.IEnumerable inEnum)
            {
                foreach (var item in inEnum.OfType<Dictionary<string, object>>())
                {
                    comp.inputConnectors.Add(ParseConnector(item));
                }
            }

            return comp;
        }

        private static SactConnectorData ParseConnector(Dictionary<string, object> dict)
        {
            SactConnectorData conn = new();
            if (dict.TryGetValue("uId", out object? uidObj) && uidObj is string uidStr)
                conn.uId = uidStr;
            if (dict.TryGetValue("PartnerUId", out object? partnerObj) && partnerObj is string partnerStr)
                conn.PartnerUId = partnerStr;
            return conn;
        }

        private static CompareState GetState(Dictionary<string, object> dict)
        {
            if (dict.TryGetValue("State", out object? stateObj) && stateObj is string stateStr)
                return ParseCompareState(stateStr);
            return CompareState.Equal;
        }

        private static Dictionary<string, object>? GetDict(Dictionary<string, object> dict, string key)
        {
            if (dict.TryGetValue(key, out object? val) && val is Dictionary<string, object> d)
                return d;
            return null;
        }

        private static CompareState ParseCompareState(string state)
        {
            switch (state)
            {
                case "Changed":
                case "Different": return CompareState.Changed;
                case "MissingOnLeft": return CompareState.MissingOnLeft;
                case "MissingOnRight": return CompareState.MissingOnRight;
                case "Equal":
                default: return CompareState.Equal;
            }
        }
    }
}
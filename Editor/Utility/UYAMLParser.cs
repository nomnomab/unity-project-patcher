using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

namespace Lachee.Utilities.Serialization
{
    /// <summary>
    /// Simple Parser for Unity YAML files. Able to produce basic tree structures, it is suitable for raw manipulation of the data but not serialization.
    /// </summary>
    public sealed class UYAMLParser
    {
        internal const string COMPONENT_HEADER = "--- !u!";

        private IUPropertyCollection _curObject;
        private UProperty _curProperty;
        private Stack<IUPropertyCollection> _objects;

        private int _spt;
        private int _indentLevel;
        private int _prevIndentLevel;

        private UYAMLParser()
        {
            _spt = 0;
            _objects = new Stack<IUPropertyCollection>();
            Reset();
        }

        /// <summary>Resets the state of the parser</summary>
        private void Reset()
        {
            _curObject = null;
            _curProperty = null;
            _objects.Clear();
            _indentLevel = 0;
            _prevIndentLevel = 0;
        }

        /// <summary>Parses the given UYAML content</summary>
        public static List<UComponent> Parse(string content)
        {
            int offset;
            int nextOffset;
            string block;

            // Get to first chunk
            offset = content.IndexOf(COMPONENT_HEADER);
            if (offset == -1)
                throw new System.InvalidOperationException("There was no blocks found");

            List<UComponent> components = new List<UComponent>();
            UYAMLParser parser = new UYAMLParser();

            do
            {
                nextOffset = content.IndexOf(COMPONENT_HEADER, offset + COMPONENT_HEADER.Length);
                if (nextOffset == -1)
                    block = content.Substring(offset);
                else
                    block = content.Substring(offset, nextOffset - offset-1);

                var component = parser.ParseComponent(block);
                components.Add(component);

                offset = nextOffset;
            } while (offset >= 0);

            return components;
        }

        private UComponent ParseComponent(string content)
        {
            Reset();
            int offset;
            int nextOffset;
            string line;

            // Get to the first line
            offset = content.IndexOf(COMPONENT_HEADER);
            if (offset == -1)
                throw new System.InvalidOperationException("The block is missing the content header");

            do
            {
                nextOffset = content.IndexOf('\n', offset)+1;
                if (nextOffset <= 0) line = content.Substring(offset);
                else line = content.Substring(offset, nextOffset - offset -1);

                ParseLine(line);

                offset = nextOffset;
            } while (offset > 0);

            while (_objects.TryPop(out var node))
            {
                if (node is UComponent comp)
                    return comp;
            }
            return null;
        }

        private void ParseLine(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return;

            string line;
            bool isArrayEntry = false;

            _prevIndentLevel = _indentLevel;
            _indentLevel = Tabulate(content, out line);
            if (_spt < 1)  // Update the indentation level based of the first item we meet
            {
                _spt = _indentLevel;
                _indentLevel = Tabulate(content, out line);
            }


            // No block yet, expecting a new def
            if (_curObject == null)
            {
                if (!line.StartsWith(COMPONENT_HEADER))
                    throw new ParseException($"Expecting a new block, but got '{line}' instead.");

                string[] segs = line.Split(' ');
                if (segs.Length != 3)
                    throw new ParseException("Expecting 3 parts in the block header.");

                _curObject = new UComponent()
                {
                    classID = (UClassID) int.Parse(segs[1].Substring(3)),
                    fileID = long.Parse(segs[2].Substring(1))
                };
                return;
            }

            // Start of Array
            if (line[0] == '-')
            {
                line = line.Trim('-', '\t', ' ');
                _indentLevel++;
                isArrayEntry = true;
            }

            // Start of Object
            int indentDiff = _indentLevel - _prevIndentLevel;
            if (indentDiff == 1)    // Something wrong with this logic when adding item,s to array
            {
                if (isArrayEntry)
                {
                    // We have increased our indentation and we have started with a -,
                    // that means we starting an array and the parent property needs to be converted 
                    // into an array.

                    // We need to convert the current property to an array
                    var arr = new UArray();
                    _objects.Push(_curObject);
                    _curProperty.value = arr;
                    _curObject = arr;
                }
                else
                {
                    // Our indentation level has increased so we are making a new object
                    var obj = new UObject();
                    _objects.Push(_curObject);
                    _curProperty.value = obj;
                    _curObject = obj;
                }
            } 
            else if (indentDiff < 0)
            {
                while (_objects.Count > _indentLevel)
                    _curObject = _objects.Pop();
            } 
            else if (indentDiff != 0)
            {
                throw new ParseException("Indentation grew/shrunk too rapidly. Expected only a change of either 0 or 1.");
            }

            // Seperate the parts
            string[] parts = line.Split(':', 2);
            if (parts.Length == 1)
            {
                if (_curObject is UArray arr)
                    arr.Add(ParseValue(line));
                else
                    throw new ParseException("Cannot add key-less values to an object");
            }
            else
            {

                _curProperty = new UProperty();
                if (parts.Length != 2)
                    throw new ParseException($"Cannot find property name in '{line}'");
                _curProperty.name = parts[0].Trim();
                _curProperty.value = ParseValue(parts[1]);

                if (isArrayEntry)
                {
                    if (_curObject is not UArray && _objects.Peek() is UArray)
                        _curObject = _objects.Pop();

                    if (_curObject is UArray arr)
                    {
                        // Create a new object to put this property into
                        var obj = new UObject();
                        _objects.Push(_curObject);
                        _curObject = obj;
                        arr.Add(obj);
                    } 
                    else
                    {
                        throw new ParseException("Adding a new array item, but could not get an array to put it in.");
                    }
                }

                // Attempt to push the current property into the current object
                if (_curProperty != null && !_curObject.Add(_curProperty))
                    throw new ParseException($"Failed to add the property '{_curProperty.name}'");
            }

#if false
            // If we are in the middle of an array, we need to either add to the 
            //  array or build the items
            if (_curObject is UArray curArray)
            {
                // Seperate the parts
                bool isProperty = line.IndexOf(':') > 0;
                bool isInlineShape = line[0] == '{' || line[0] == '[';

                if (!isProperty && !isInlineShape)   // A single value, we will push it to the array directly
                {
                    if (!isArrayEntry)
                        throw new ParseException("Array Values must begin with -");
                    curArray.Add(ParseValue(line));
                }
                else if (isInlineShape)
                {
                    if (!isArrayEntry)
                        throw new ParseException("Array Values must begin with -");
                    curArray.Add(ParseValue(line));
                }
                else // An object for a value
                {
                    if (isArrayEntry) // Its a new item, so create the item and push it to the array.
                    {
                        var item = new UObject();
                        curArray.Add(item);
                    }

                    string[] parts = line.Split(':');
                    if (parts.Length == 2)  // If we have a name: value, add it to the last array item.
                    {
                        var lastItem = curArray.items[curArray.items.Count - 1];
                        if (lastItem is IUPropertyCollection propertyCollection)
                        {
                            var property = new UProperty()
                            {
                                name = parts[0].Trim(),
                                value = ParseValue(parts[1])
                            };

                            if (!propertyCollection.Add(property))
                                throw new ParseException($"Duplicate property found '{property.name}'");
                        }
                        else
                        {
                            throw new ParseException("Previous array item was not a property collection. Cannot add new properties.");
                        }
                    }
                }
            }
            else  // Adding a property to the previous object.
            {
                // Seperate the parts
                string[] parts = line.Split(':', 2);

                _curProperty = new UProperty();
                if (parts.Length != 2)
                    throw new ParseException($"Cannot find property name in '{line}'");
                _curProperty.name = parts[0].Trim();
                _curProperty.value = ParseValue(parts[1]);

                // Attempt to push the current property into the current object
                if (_curProperty != null && !_curObject.Add(_curProperty))
                    throw new ParseException($"Duplicate property found '{_curProperty.name}'");
            }
#endif
        }

        private UNode ParseValue(string value)
        {
            string[] parts;
            string content = value.Trim();

            if (content.Length == 0)
                return new UValue();

            if (content[0] == '{' && content[content.Length - 1] == '}') {
                UObject objValue = new UObject();
                if (content.Length > 2)
                {
                    parts = content.Split(',');
                    foreach (var part in parts)
                    {
                        string[] sParts = part.Trim('{', '}', ' ').Split(':', 2);
                        if (sParts.Length != 2)
                            throw new ParseException("Cannot parse non-key values inside a inline object!");
                        objValue.Add(new UProperty()
                        {
                            name = sParts[0].Trim(),
                            value = ParseValue(sParts[1])
                        });
                    }
                }
                return objValue;
            }

            if (content[0] == '[' && content[content.Length - 1] == ']') {
                UArray arrayValue = new UArray();
                if (content.Length > 2)
                {
                    parts = content.Split(',');
                    foreach (var part in parts)
                        arrayValue.Add(new UValue(part.Trim('[', ']', ' ')));
                }
                return arrayValue;
            }

            if (content[0] == '"' && content[content.Length - 1] == '"')
                content = content.Trim('"').Replace("\\\"", "\"");

            return new UValue(content);
        }


        private int Tabulate(string content, out string line)
        {
            int spaces;
            for (spaces = 0; spaces < content.Length; spaces++)
            {
                if (content[spaces] != ' ' && content[spaces] != '\t')
                    break;
            }

            line = content.TrimStart(' ', '\t', '\n', '\r');
            return spaces / Math.Max(_spt, 1);
        }
    }

    /// <summary>Represents errors that occure during parsing</summary>
    public sealed class ParseException : System.Exception
    {
        public ParseException(string message) : base(message) { }
    }

}

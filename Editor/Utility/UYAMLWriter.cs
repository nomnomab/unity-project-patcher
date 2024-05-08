using System.Collections.Generic;
using System.Text;

namespace Lachee.Utilities.Serialization
{
    public class UYAMLWriter
    {
        private const string DEFAULT_EOL = "\r\n";
        private StringBuilder builder = new StringBuilder();

        private string eol = DEFAULT_EOL;

        public int IndentationSpaces { get; set; } = 2;
        public bool InlineObjects { get; set; } = true;
        public bool InlineComplexObjects { get; set; } = false;
        public bool InlineArrays { get; set; } = true;
        public bool InlineComplexArrays { get; set; } = false;

        public void AddComponets(IEnumerable<UComponent> components)
        {
            foreach (var component in components)
                AddComponent(component);
        }
        public void AddComponent(UComponent component)
        {
            builder.Append(UYAMLParser.COMPONENT_HEADER)
                .Append((int)component.classID)
                .Append(" &")
                .Append(component.fileID)
                .Append(eol);

            AppendProperty(component.Property, 0, false, false);
        }

        private void AppendProperty(UProperty property, int indent, bool arrayItem, bool skipIndent)
        {
            int indentLevel = indent;
            int indentSize = IndentationSpaces;
            string name = string.IsNullOrEmpty(property.name) ? "" : property.name + ":";

            if (arrayItem)
            {
                name = "-";
                indentLevel -= 1;
            }

            if (skipIndent)
                indentSize = 0;

            switch (property.value)
            {
                default: break;
                case UValue uValue:
                    builder.Append(' ', indentLevel * indentSize).Append(name).Append(" ").Append(uValue.value);
                    builder.Append(eol);
                    break;

                case UObject uObject:
                    if (CanInline(uObject) && !arrayItem)
                    {
                        builder.Append(' ', indentLevel * indentSize).Append(name).Append(" {");
                        eol = "";
                        bool first = true;
                        foreach (var kp in uObject.properties)
                        {
                            if (!first) builder.Append(", ");
                            AppendProperty(kp.Value, 0, false, true);
                            first = false;
                        }
                        eol = DEFAULT_EOL;
                        builder.Append('}').Append(eol);
                    }
                    else
                    {
                        bool first = true;
                        builder.Append(' ', indentLevel * indentSize).Append(name);
                        builder.Append(arrayItem ? ' ' : eol);  // This inlines the first time of an array
                        foreach (var kp in uObject.properties)
                        {
                            AppendProperty(kp.Value, indentLevel + 1, false, arrayItem && first);
                            first = false;
                        }
                    }
                    break;

                case UArray uArray:
                    if (CanInline(uArray))
                    {
                        builder.Append(' ', indentLevel * indentSize).Append(name).Append(" [");
                        eol = "";
                        bool first = true;
                        for (int i = 0; i < uArray.items.Count; i++)
                        {
                            if (!first) builder.Append(",");
                            AppendProperty(new UProperty() { value = uArray.items[i] }, 0, false, true);
                            first = false;
                        }

                        eol = DEFAULT_EOL;
                        builder.Append(']').Append(eol);
                    }
                    else
                    {
                        builder.Append(' ', indentLevel * indentSize).Append(name);
                        builder.Append(eol);
                        for (int i = 0; i < uArray.items.Count; i++)
                            AppendProperty(new UProperty() { value = uArray.items[i] }, indentLevel + 1, true, false);
                    }
                    break;
            }
        }

        private bool CanInline(UObject obj)
        {
            if (!InlineObjects)
                return false;

            if (obj.properties.Count == 0)
                return true;

            foreach(var kp in obj.properties)
            {
                if (InlineComplexObjects)
                {
                    if (kp.Value.value is UObject o && !CanInline(o))
                        return false;
                    if (kp.Value.value is UArray a && !CanInline(a))
                        return false;
                }
                else
                {
                    if (kp.Value.value is not UValue)
                        return false;
                }
            }

            return true;
        }

        private bool CanInline(UArray arr)
        {
            if (!InlineArrays) 
                return false;

            if (arr.items.Count == 0)
                return true;
            foreach(var item in arr.items)
            {
                if (InlineComplexArrays)
                {
                    if (item is UObject o && !CanInline(o))
                        return false;
                    if (item is UArray a && !CanInline(a))
                        return false;
                } 
                else 
                {
                    if (item is not UValue)
                        return false;
                }
            }
            return true;
        }

        public override string ToString()
            => $"%YAML 1.1{DEFAULT_EOL}%TAG !u! tag:unity3d.com,2011:{DEFAULT_EOL}" + builder.ToString();        
    }
}

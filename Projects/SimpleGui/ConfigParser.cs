using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleGui.ConfigParser
{
    public class ConstraintElement : ConfigurationElement
    {
        [ConfigurationProperty("id", IsRequired = true, IsKey = true)]
        public int Id
        {
            get { return (int)base["id"]; }
        }

        [ConfigurationProperty("name", IsRequired = true)]
        public string Name
        {
            get { return (string)base["name"]; }
        }

        [ConfigurationProperty("value", IsRequired = true)]
        public string Value
        {
            get { return (string)base["value"]; }
        }

        [ConfigurationProperty("volume", IsRequired = false)]
        public string Volume
        {
            get { return (string)base["volume"]; }
        }

        [ConfigurationProperty("weight", IsRequired = true)]
        public string Weight
        {
            get { return (string)base["weight"]; }
        }
    }
    public class ConstraintElementCollection : ConfigurationElementCollection
    {
        internal const string PropertyName = "constraint";

        public override ConfigurationElementCollectionType CollectionType
        {
            get
            {
                return ConfigurationElementCollectionType.BasicMapAlternate;
            }
        }
        protected override string ElementName
        {
            get
            {
                return PropertyName;
            }
        }

        protected override bool IsElementName(string elementName)
        {
            return elementName.Equals(PropertyName,
              StringComparison.InvariantCultureIgnoreCase);
        }

        public override bool IsReadOnly()
        {
            return false;
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new ConstraintElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((ConstraintElement)(element)).Id;
        }

        public ConstraintElement this[int idx]
        {
            get { return (ConstraintElement)BaseGet(idx); }
        }
    }
    public class ConstraintList : ConfigurationSection
    {
        [ConfigurationProperty("constraints")]
        public ConstraintElementCollection Constraints
        {
            get { return ((ConstraintElementCollection)(base["constraints"])); }
            set { base["constraints"] = value; }
        }
    }
}



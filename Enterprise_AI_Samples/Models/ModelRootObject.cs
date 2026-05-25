namespace CAE_AI_Samples.Models
{
    public class ModelRootObject
    {
        public string uuid { get; set; }
        public bool overwriteSniExtension { get; set; }
        public string name { get; set; }
        public int passwordIteration { get; set; }
        public Modelspecifictemplate[] modelSpecificTemplates { get; set; }
        public Permission[] permissions { get; set; }
    }

    public class Modelspecifictemplate
    {
        public string uuid { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public string enumType { get; set; }
        public string defaultValue { get; set; }
        public string description { get; set; }
        public Enumvalue[] enumValues { get; set; }
    }

    public class Enumvalue
    {
        public string name { get; set; }
        public string value { get; set; }
        public int order { get; set; }
    }

    public class Permission
    {
        public string uuid { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string[] roles { get; set; }
    }
}

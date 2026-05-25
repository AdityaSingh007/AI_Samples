namespace CAE_AI_Samples.Models
{
    public class ModelDetail
    {
        public int ModelId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Firmware { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int PasswordIteration { get; set; } = 0;
        public bool OverwriteSniExtension { get; set; } = false;
        public List<ModelPermission> Permissions { get; set; } = new List<ModelPermission>();

    }
}

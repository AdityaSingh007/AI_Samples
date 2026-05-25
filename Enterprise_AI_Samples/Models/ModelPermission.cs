using System;
using System.Collections.Generic;
using System.Text;

namespace CAE_AI_Samples.Models
{
    public class ModelPermission
    {
        public int PermissionId { get; set; }
        public bool HasObjects { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
    }
}

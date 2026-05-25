using System;
using System.Collections.Generic;
using System.Text;

namespace CAE_AI_Samples.Models
{
    public sealed class ProjectImportRequest
    {
        public required byte[] ProjectContent { get; init; }
        public required string Password { get; init; }
        public required bool UpgradeModelDefinition { get; init; }
    }
}

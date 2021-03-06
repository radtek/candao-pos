﻿using System.Collections.Generic;

namespace CnSharp.Delivery.VisualStudio.PackingTool
{
    public class ProjectCache
    {
        public ProjectCache()
        {
            ExcludeFiles = new List<string>();
            ExcludeFolders = new List<string>();
        }
        public List<string> ExcludeFolders { get; set; }
        public List<string> ExcludeFiles { get; set; }
    }
}

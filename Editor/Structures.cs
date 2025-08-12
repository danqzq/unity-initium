using System.Collections.Generic;

namespace Danqzq.Initium
{
    [System.Serializable]
    internal class ItemConfig : System.IEquatable<ItemConfig>
    {
        public string name;
        public bool include;

        internal string Name => name;

        internal bool Include
        {
            get => include;
            set => include = value;
        }

        protected ItemConfig(string name, bool include)
        {
            this.name = name;
            Include = include;
        }

        public bool Equals(ItemConfig other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Name == other.Name && Include == other.Include;
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((ItemConfig)obj);
        }

        public override int GetHashCode()
        {
            return System.HashCode.Combine(Name);
        }
    }
    
    [System.Serializable]
    internal class FolderConfig : ItemConfig
    {
        internal FolderConfig(string name, bool include) : base(name, include) { }
    }
    
    [System.Serializable]
    internal class PackageConfig : ItemConfig
    {
        internal PackageConfig(string name, bool include) : base(name, include) { }
    }
    
    [System.Serializable]
    internal class PackageFileConfig : ItemConfig
    {
        internal PackageFileConfig(string name, bool include) : base(name, include) { }
    }
    
    [System.Serializable]
    internal struct InitiumConfig
    {
        public string baseNamespace;
        public FolderConfig[] ScriptsFolders;
        public FolderConfig[] AudioFolders;
        public List<PackageConfig> Packages;
        public List<PackageFileConfig> PackageFiles;
    }
}
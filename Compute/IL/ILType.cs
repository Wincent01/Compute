namespace Compute.IL
{
    public class ILType
    {
        public bool IsGlobal { get; set; }
        
        public bool IsConst { get; set; }
        
        public bool IsPointer { get; set; }
        
        public string Type { get; set; }
    }
}
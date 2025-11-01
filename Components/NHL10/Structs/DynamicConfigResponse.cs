using Tdf;

namespace Zamboni.Components.NHL10.Structs
{
    [TdfStruct]
    public struct DynamicConfigResponse
    {
        [TdfMember("CDRD")]
        public ushort mCDRD;
        
        [TdfMember("CERD")]
        public ushort mCERD;
        
        [TdfMember("CMDI")]
        public ushort mCMDI;
        
        [TdfMember("CMMC")]
        public ushort mCMMC;
    }
}
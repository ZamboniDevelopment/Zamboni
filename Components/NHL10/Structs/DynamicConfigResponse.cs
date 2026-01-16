using Tdf;

namespace Zamboni.Components.NHL10.Structs
{
    [TdfStruct]
    public struct DynamicConfigResponse
    {
        [TdfMember("CDRD")]
        public ushort mDataRequestDelay;
        
        [TdfMember("CERD")]
        public ushort mErrorRetryDelay;
        
        [TdfMember("CMDI")]
        public ushort mMessageDelayInterval;
        
        [TdfMember("CMMC")]
        public ushort mMaximumMessageCount;
    }
}
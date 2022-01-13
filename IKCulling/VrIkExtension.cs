//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Runtime.CompilerServices;
//using System.Text;
//using System.Threading.Tasks;
//using FrooxEngine.FinalIK;

//namespace IkCulling
//{
//    public static class VrIkExtension
//    {
//        private static ConditionalWeakTable<VRIK, IkThrottleData> _ikThrottleDataTable =
//            new ConditionalWeakTable<VRIK, IkThrottleData>();

//        public static IkThrottleData GetThrottleData(this VRIK vrik)
//        {
//            return _ikThrottleDataTable.GetOrCreateValue(vrik);
//        }

//        public static IkThrottleData AddThrottleData(this VRIK vrik)
//        {
//            var data = new IkThrottleData();
//            _ikThrottleDataTable.Add(vrik, data);
//            return data;
//        }
//    }
//}


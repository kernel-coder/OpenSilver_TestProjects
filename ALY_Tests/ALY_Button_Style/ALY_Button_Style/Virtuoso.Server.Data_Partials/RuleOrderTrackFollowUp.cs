#region Usings

using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Virtuoso.Core.Cache;

#endregion

namespace Virtuoso.Server.Data
{
    public partial class RuleOrderTrackFollowUp
    {
        private List<InitSub> initSubList;
        private List<CodeLookup> orderTypes;

        public IOrderedEnumerable<InitSub> FilteredInitSubList
        {
            get
            {
                IOrderedEnumerable<InitSub> filtered = null;

                if (RuleHeader != null
                    && RuleHeader.RuleOrderTrackFollowUp != null
                    && RuleHeader.RuleOrderTrackFollowUp.Any(r =>
                        r.RuleOrderTrackFollowUpKey != RuleOrderTrackFollowUpKey)
                   )
                {
                    filtered = InitSubList.Where(i => !RuleHeader.RuleOrderTrackFollowUp.Where(r =>
                            r.RuleOrderTrackFollowUpKey != RuleOrderTrackFollowUpKey
                            && r.OrderType == OrderType
                        ).Select(r => r.InitialOrSubsequent).Contains(i.Code))
                        .OrderBy(i => i.Description);
                }
                else
                {
                    filtered = InitSubList.OrderBy(i => i.Description);
                }

                return filtered;
            }
        }

        public List<InitSub> InitSubList
        {
            get
            {
                if (initSubList == null)
                {
                    initSubList = new List<InitSub>
                    {
                        new InitSub { Code = "I", Description = "Initial" },
                        new InitSub { Code = "S", Description = "Subsequent" }
                    };
                }

                return initSubList;
            }
        }

        public IOrderedEnumerable<CodeLookup> FilteredOrderTypes
        {
            get
            {
                IOrderedEnumerable<CodeLookup> filtered = null;

                // we want to remove all OrderTypes that already have both an initial and subsequent defined
                if (RuleHeader != null
                    && RuleHeader.RuleOrderTrackFollowUp != null
                    && RuleHeader.RuleOrderTrackFollowUp.Any(r =>
                        r.RuleOrderTrackFollowUpKey != RuleOrderTrackFollowUpKey)
                   )
                {
                    var headers = RuleHeader.RuleOrderTrackFollowUp.Where(
                        r => r.RuleOrderTrackFollowUpKey != RuleOrderTrackFollowUpKey
                    );

                    filtered = OrderTypes
                        .Where(i => !headers.Any(h => h.OrderType == i.CodeLookupKey && h.InitialOrSubsequent == "I")
                                    || !headers.Any(h => h.OrderType == i.CodeLookupKey && h.InitialOrSubsequent == "S"))
                        .OrderBy(i => i.CodeDescription);
                }
                else
                {
                    filtered = OrderTypes.OrderBy(i => i.CodeDescription);
                }

                return filtered;
            }
        }

        public List<CodeLookup> OrderTypes
        {
            get
            {
                if (orderTypes == null)
                {
                    orderTypes = CodeLookupCache.GetCodeLookupsFromType("OrderType", true, false, true);
                }

                return orderTypes;
            }
        }

        public string OrderTypeOrderBy
        {
            get
            {
                string orderTypeOrderBy = null;

                var cl = CodeLookupCache.GetCodeLookupFromKey(OrderType);
                orderTypeOrderBy = cl == null ? "ZZZZZ" : cl.CodeDescription;
                return orderTypeOrderBy;
            }
        }

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.PropertyName == "OrderType")
            {
                RaisePropertyChanged("FilteredInitSubList");
            }

            if (e.PropertyName == "InitialOrSubsequent")
            {
                RaisePropertyChanged("FilteredOrderTypes");
            }
        }

        public class InitSub
        {
            public string Code { get; set; }
            public string Description { get; set; }
        }
    }
}
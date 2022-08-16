#region Usings

using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using OpenRiaServices.DomainServices.Client;
using Virtuoso.Core;

#endregion

namespace Virtuoso.Server.Data
{
    public partial class ServiceLineGroupHeader
    {
        //Setup Property Changes Handlers on first time
        private bool HandlersHasBeenSetup;


        public bool IsRootHeader => SequenceNumber != null && SequenceNumber != 0;


        public List<GroupNode> ServiceLineGroupsTree
        {
            get
            {
                if (ApplicationCoreContext.ServiceLineService == null)
                {
                    return null;
                }

                if (ApplicationCoreContext.ServiceLineService.Context == null)
                {
                    return null;
                }

                //Setup Property Changes Handlers on first time
                SetupHandlersOnTreeChanges();

                var activeGroupNodesInServiceLine = ApplicationCoreContext.ServiceLineService.Context
                    .ServiceLineGroupings
                    .Where(g => g.Inactive == false)
                    .Where(g => g.ServiceLineKey == ServiceLine.ServiceLineKey)
                    .Select(g => new GroupNode
                    {
                        Key = g.ServiceLineGroupingKey,
                        GroupName = g.Name,
                        Header = g.ServiceLineGroupHeader.GroupHeaderLabel,
                        Level = g.ServiceLineGroupHeader.SequenceNumber ?? 0
                    })
                    .OrderBy(g => g.Header)
                    .ThenBy(g => g.GroupName)
                    .ToList();

                var allRelationshipsBetweenGroupsOfTheSameServiceLine = ApplicationCoreContext.ServiceLineService
                    .Context.ServiceLineGroupingParents
                    .Where(p => p.ServiceLineGrouping != null && p.ServiceLineGrouping1 != null)
                    .Where(p => p.ServiceLineGrouping.ServiceLineKey == ServiceLineKey ||
                                p.ServiceLineGrouping1.ServiceLineKey == ServiceLineKey);

                var allRootGroupsInServiceLine = activeGroupNodesInServiceLine.Where(g => g.Level == 0).ToList();
                var groupsTree = GetDescendents(allRootGroupsInServiceLine,
                    allRelationshipsBetweenGroupsOfTheSameServiceLine, activeGroupNodesInServiceLine).ToList();

                //Indicate that Header is duplicate when compared to previous value (for formatting purposes)
                var prevHeader = string.Empty;
                foreach (var treeNode in groupsTree)
                {
                    treeNode.PrevHeaderHasSameValue = prevHeader == treeNode.Header;
                    prevHeader = treeNode.Header;
                }

                return groupsTree;
            }
        }

        private void ServiceLineGroupHeader_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "GroupHeaderLabel")
            {
                RaisePropertyChanged("ServiceLineGroupsTree");
            }
        }

        partial void OnSequenceNumberChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("IsRootHeader");
        }

        private void SetupHandlersOnTreeChanges()
        {
            if (HandlersHasBeenSetup)
            {
                return;
            }

            if (ApplicationCoreContext.ServiceLineService == null)
            {
                return;
            }

            if (ApplicationCoreContext.ServiceLineService.Context == null)
            {
                return;
            }

            ApplicationCoreContext.ServiceLineService.Context.ServiceLineGroupHeaders.ToList()
                .ForEach(h => h.PropertyChanged += ServiceLineGroupHeader_PropertyChanged);

            ApplicationCoreContext.ServiceLineService.Context.ServiceLineGroupingParents.EntityAdded +=
                ServiceLineGroupingParents_EntityAddedDeleted;
            ApplicationCoreContext.ServiceLineService.Context.ServiceLineGroupingParents.EntityRemoved +=
                ServiceLineGroupingParents_EntityAddedDeleted;

            ApplicationCoreContext.ServiceLineService.Context.ServiceLineGroupHeaders.EntityAdded +=
                ServiceLineGroupHeaders_EntityAddedDeleted;
            ApplicationCoreContext.ServiceLineService.Context.ServiceLineGroupHeaders.EntityRemoved +=
                ServiceLineGroupHeaders_EntityAddedDeleted;


            HandlersHasBeenSetup = true;
        }

        private void ServiceLineGroupHeaders_EntityAddedDeleted(object sender,
            EntityCollectionChangedEventArgs<ServiceLineGroupHeader> e)
        {
            RaisePropertyChanged("ServiceLineGroupsTree");
        }

        private void ServiceLineGroupingParents_EntityAddedDeleted(object sender,
            EntityCollectionChangedEventArgs<ServiceLineGroupingParent> e)
        {
            RaisePropertyChanged("ServiceLineGroupsTree");
        }

        private static IEnumerable<GroupNode> GetDescendents(IEnumerable<GroupNode> parentNodesList,
            IEnumerable<ServiceLineGroupingParent> groupRelationships, IEnumerable<GroupNode> groupsList)
        {
            var parentsPlustDescendents = new List<GroupNode>();
            var groupRelationshipsList = groupRelationships.ToList();
            var groupsListList = groupsList.ToList();
            foreach (var parentNode in parentNodesList)
                parentsPlustDescendents.AddRange(GetDescendents(parentNode, groupRelationshipsList, groupsListList));
            return parentsPlustDescendents;
        }

        private static IEnumerable<GroupNode> GetDescendents(GroupNode parentNode,
            IEnumerable<ServiceLineGroupingParent> groupRelationships, IEnumerable<GroupNode> groupsList)
        {
            var parentPlusDescendents = new List<GroupNode> { parentNode };
            var enumeratedGroupRelationShips = groupRelationships.ToList();
            var enumeratedGroupList = groupsList.ToList();

            var childrenIDs = enumeratedGroupRelationShips
                .Where(r => r.ServiceLineGroupingKey != parentNode.Key) //child can not be its own parent
                .Where(r => r.ParentServiceLineGroupingKey == parentNode.Key)
                .Select(p => p); //list only relationships with parentNode as parent

            //Get descendent's ServiceLineGrouping 
            var firstDegreeDescendents = childrenIDs
                .Join(enumeratedGroupList, r => r.ServiceLineGroupingKey, g => g.Key, (r, g) => g) //
                .OrderBy(g => g.Header)
                .ThenBy(g => g.GroupName)
                .ToList();


            var allDescendents =
                GetDescendents(firstDegreeDescendents, enumeratedGroupRelationShips, enumeratedGroupList);

            parentPlusDescendents.AddRange(allDescendents);

            return parentPlusDescendents;
        }

        public void RaiseEvents()
        {
            RaisePropertyChanged(null);
        }
    }

    public class GroupNode
    {
        public int Key { get; set; }
        public string Offset => new string(' ', Level * 3);
        public string Header { get; set; }
        public string GroupName { get; set; }
        public int Level { get; set; }

        //Indicate that the Header value in previous row has same value for formatting purposes
        public bool PrevHeaderHasSameValue
        {
            get;
            set;
        }

        public string FormattedHeader
        {
            get
            {
                //Replace Header Value with spaces if it is equal to previous one
                if (PrevHeaderHasSameValue)
                {
                    return new string(' ', Header.Length);
                }

                return Header;
            }
        }

        public override string ToString()
        {
            return new string(' ', Level * 6) + Header + ": " + GroupName;
        }
    }
}
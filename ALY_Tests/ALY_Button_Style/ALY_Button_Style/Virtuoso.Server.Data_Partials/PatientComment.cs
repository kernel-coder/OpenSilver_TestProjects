#region Usings

using System;

#endregion

namespace Virtuoso.Server.Data
{
    public interface ICommentItem
    {
        string Comment { get; set; }
        bool Deleted { get; set; }
        DateTime EntryDateTime { get; set; }
        bool HasChanges { get; }
        bool IsNew { get; }
        bool IsPatient { get; }
        void BeginEditting();
        void CancelEditting();
        void EndEditting();
    }

    public class CommentItem : object
    {
        public CommentItem()
        {
            CommentObject = null;
        }

        public CommentItem(ICommentItem commentObject)
        {
            CommentObject = commentObject;
        }

        public ICommentItem CommentObject { get; set; }

        public string Comment
        {
            get
            {
                var co = CommentObject;
                return co?.Comment;
            }
            set
            {
                var co = CommentObject;
                if (co == null)
                {
                    return;
                }

                co.Comment = value;
                ;
            }
        }

        public bool Deleted
        {
            get
            {
                var co = CommentObject;
                return co?.Deleted ?? false;
            }
            set
            {
                var co = CommentObject;
                if (co == null)
                {
                    return;
                }

                co.Deleted = value;
            }
        }

        public DateTime? EntryDateTime
        {
            get
            {
                var co = CommentObject;
                return co?.EntryDateTime;
            }
            set
            {
                var co = CommentObject;
                if (co == null)
                {
                    return;
                }

                co.EntryDateTime = value.Value;
                ;
            }
        }

        public bool HasChanges
        {
            get
            {
                var ve = CommentObject as VirtuosoEntity;
                return ve?.HasChanges ?? false;
            }
        }

        public bool IsNew
        {
            get
            {
                var ve = CommentObject as VirtuosoEntity;
                return ve?.IsNew ?? false;
            }
        }

        public bool IsPatient
        {
            get
            {
                var co = CommentObject as PatientComment;
                return co != null;
            }
        }

        public void BeginEditting()
        {
            var ve = CommentObject as VirtuosoEntity;
            if (ve != null)
            {
                ve.BeginEditting();
            }
        }

        public void CancelEditting()
        {
            var ve = CommentObject as VirtuosoEntity;
            if (ve != null)
            {
                ve.CancelEditting();
            }
        }

        public void EndEditting()
        {
            var ve = CommentObject as VirtuosoEntity;
            if (ve != null)
            {
                ve.EndEditting();
            }
        }
    }

    public partial class PatientComment : ICommentItem
    {
        public bool IsPatient => true;
    }

    public partial class TaskComment : ICommentItem
    {
        public bool IsPatient => false;
    }
}
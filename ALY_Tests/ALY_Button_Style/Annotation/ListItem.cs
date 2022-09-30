namespace Annotation
{
    public class ListItem
    {
        public int Id { get; internal set; }
        public string Description { get; internal set; }
        public bool IsSelected { get; set; }

        internal ListItem(int id, string description, bool isselected)
        {
            this.Id = id;
            this.Description = description;
            this.IsSelected = isselected;
        }
    }
}

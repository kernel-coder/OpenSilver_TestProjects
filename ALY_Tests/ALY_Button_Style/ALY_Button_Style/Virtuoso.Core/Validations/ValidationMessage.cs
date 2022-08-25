namespace Virtuoso.Core.Validations
{
    public struct ValidationMessage
    {
        public string ID { get; set; }
        public string Template { get; set; }

        public string ToText(params object[] args)
        {
            return string.Format(ID + ": " + Template, args);
        }

        public string Explanaton { get; set; }
        public string UserAction { get; set; }
        public string[] MemberNames { get; set; }

        public override string ToString()
        {
            return ToText();
        }
    }
}
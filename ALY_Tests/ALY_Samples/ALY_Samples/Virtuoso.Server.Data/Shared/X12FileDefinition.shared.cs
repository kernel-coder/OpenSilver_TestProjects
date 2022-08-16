using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Reflection;
using System.IO;

namespace Virtuoso.Server.Data
{
    public partial class X12File
    {
		public List<Loop> Loops = new List<Loop>();
    }

    public partial class Loop
    {
        public string SegmentSeparator = null;
		public List<X12Segment> Segments = new List<X12Segment>();  
     
		public List<Loop> Loops = new List<Loop>();

		public string Name;
		public Loop Parent;
		public Loop NextSibling;
		
		public override string ToString()
		{
			string loopAsString = null;

			foreach (X12Segment s in Segments)
			{
				loopAsString += s.SegmentString + "~";
			}

			foreach (Loop l in Loops)
			{
				loopAsString += l.ToString();
			}

			return loopAsString;
		}

        public bool ContainsMaxNumSegments(X12FileStructure.SegmentStructure SegmentStructure)
        {
            bool maxedOut = false;

            if (SegmentStructure.Max > 0)
            {
                maxedOut = (from seg in Segments
                            where seg.UniqueID == SegmentStructure.UniqueID
                            select seg
                           ).Count() >= SegmentStructure.Max;
            }

            return maxedOut;
        }

        public List<X12Segment> GetAllSegmentsFromLoopAndDescendants()
        {
            List<X12Segment> segList = new List<X12Segment>();

            foreach (Loop loop in Loops)
            {
                segList.AddRange(loop.GetAllSegmentsFromLoopAndDescendants());
            }

            segList.AddRange(Segments);

            return segList;
        }

        public List<Loop> GetAllDescendantLoopsByName(string Name)
        {
            List<Loop> loops = new List<Loop>();

            foreach (Loop loop in Loops)
            {
                loops.AddRange(loop.GetAllDescendantLoopsByName(Name));
            }

            loops.AddRange((from lp in Loops
                            where lp.Name == Name
                            select lp
                           ).ToList()
                          );
            return loops;
        }
    }

    public partial class X12Segment
    {
        public event EventHandler InvalidElementIndex = null;
        public event EventHandler InvalidComponentIndex = null;
		private int segmentNumber = -1;

		public string ElementSeperator;
		public string ComponentSeperator;
		public string RepetitionSeperator;

		public string UniqueID;
		public string SegmentID;
		public string SegmentString;
		public bool KnownSegment;
    
		public X12Segment(char? ElemSeperator, char? CompSeperator, char? RepSep, int SegNumber)
		{
			if (ElemSeperator.HasValue)
			{
				ElementSeperator = ElemSeperator.ToString();
			}
			if (CompSeperator.HasValue)
			{
				ComponentSeperator = CompSeperator.ToString();
			}
			if (RepSep.HasValue)
			{
				RepetitionSeperator = RepSep.ToString();
			}
			SegmentNumber = SegNumber;
		}

        public List<string> GetDate(int FormatElement, int? FormatComponent, int ValueElement, int? ValueComponent)
        {
            string format = null;

            if (FormatComponent.HasValue)
            {
                format = this[FormatElement, FormatComponent.Value];
            }
            else
            {
                format = this[FormatElement];
            }

            return GetDate(format, ValueElement, ValueComponent);
        }

        public string GetTime(int ValueElement, int? ValueComponent)
        {
            string timeString = null;
            string returnString = null;
            bool valid = true;
            string amPm = null;
            int hour = -1;
            int minute = -1;

            if (ValueComponent.HasValue)
            {
                timeString = this[ValueElement, ValueComponent.Value];
            }
            else
            {
                timeString = this[ValueElement];
            }

            timeString = timeString.Trim();
            if ((timeString.Length == 3)
                || (timeString.Length == 4)
               )
            {
                try
                {
                    if (timeString.Length == 3)
                    {
                        hour = int.Parse(timeString.Substring(0, 1));
                        minute = int.Parse(timeString.Substring(1, 2));
                        amPm = "AM";
                    }
                    else
                    {
                        hour = int.Parse(timeString.Substring(0, 2));
                        minute = int.Parse(timeString.Substring(2, 2));
                        amPm = "AM";
                    }
                }
                catch
                {
                    valid = false;
                }
            }
            else
            {
                valid = false;
            }

            if (valid)
            {
                if ((minute < 0)
                    || (minute > 59)
                   )
                {
                    valid = false;
                }

                if ((hour < 0)
                    || (hour > 23)
                   )
                {
                    valid = false;
                }
            }

            if (valid)
            {
                if (hour == 0)
                {
                    hour = 12;
                    amPm = "AM";
                }
                else if (hour == 12)
                {
                    amPm = "PM";
                }
                else if (hour > 12)
                {
                    hour -= 12;
                    amPm = "PM";
                }
                else
                {
                    amPm = "AM";
                }

                returnString = hour.ToString() + ":" + minute.ToString("00") + " " + amPm;
            }

            if (!valid)
            {
                returnString = timeString;
            }

            return returnString;
        }
        public List<string> GetDate(string Format, int ValueElement, int? ValueComponent)
        {
            List<string> dateList = new List<string>();
            string dateString = null;
            bool valid = false;

            if (ValueComponent.HasValue)
            {
                dateString = this[ValueElement, ValueComponent.Value];
            }
            else
            {
                dateString = this[ValueElement];
            }

            if (Format == "D8")
            {
                if (dateString.Length == 8)
                {
                    valid = true;
                    dateList.Add(FormatDateString(dateString));
                }
            }
            else if (Format == "RD8")
            {
                if (dateString.Length == 17)
                {
                    valid = true;
                    dateList.Add(FormatDateString(dateString.Substring(0, 8)));
                    dateList.Add(FormatDateString(dateString.Substring(9, 8)));
                }
            }
            else if (Format == "D6")
            {
                if (dateString.Length == 6)
                {
                    valid = true;
                    dateList.Add(FormatDateString(dateString));
                }
            }
            else if (Format == "RD6")
            {
                if (dateString.Length == 13)
                {
                    valid = true;
                    dateList.Add(FormatDateString(dateString.Substring(0, 6)));
                    dateList.Add(FormatDateString(dateString.Substring(7, 6)));
                }
            }
            else if (Format == "DT")
            {
                if (dateString.Length == 13)
                {
                    // CCYYMMDD HHMM
                    valid = true;
                    dateList.Add(FormatDateString(dateString));
                }
            }

            if (!valid)
            {
                dateList.Add(dateString);
            }

            return dateList;
        }

        public string GetName(int FirstNameElement, int? FirstNameComponent, int MiddleInitialElement, int? MiddleInitialComponent,
                              int LastNameElement, int? LastNameComponent)
        {
            string name = null;

            string firstName = null;
            string mi = null;
            string lastName = null;

            firstName = this[FirstNameElement, FirstNameComponent];
            mi = this[MiddleInitialElement, MiddleInitialComponent];
            lastName = this[LastNameElement, LastNameComponent];

            name = lastName + ", " + firstName + ((string.IsNullOrEmpty(mi))
                                                    ? null
                                                    : " " + mi);
            return name;
        }

        public string GetMoney(int ValueElement, int? ValueComponent)
        {
            string money = null;
            string initialData = null;

            try
            {
                if (ValueComponent.HasValue)
                {
                    initialData = this[ValueElement, ValueComponent.Value];
                }
                else
                {
                    initialData = this[ValueElement];
                }

                if (!string.IsNullOrEmpty(initialData))
                {
                    double doubleMoney = double.Parse(initialData);
                    money = string.Format("{0:$0.00}", doubleMoney);
                }
            }
            catch
            {
                money = initialData;
            }

            return money;
        }

        private string FormatDateString(string StringToFormat)
        {
            string formattedString = null;

            if (StringToFormat.Length == 8)
            {
                try
                {
                    int year = int.Parse(StringToFormat.Substring(0, 4));
                    int month = int.Parse(StringToFormat.Substring(4, 2));
                    int day = int.Parse(StringToFormat.Substring(6, 2));

                    formattedString = month.ToString("00") + "/" + day.ToString("00") + "/" + year.ToString("0000");
                }
                catch
                {
                    formattedString = StringToFormat;
                }
            }
            else if (StringToFormat.Length == 6)
            {
                try
                {
                    int year = int.Parse(StringToFormat.Substring(0, 2));
                    int month = int.Parse(StringToFormat.Substring(2, 2));
                    int day = int.Parse(StringToFormat.Substring(4, 2));

                    formattedString = month.ToString("00") + "/" + day.ToString("00") + "/" + year.ToString("00");
                }
                catch
                {
                    formattedString = StringToFormat;
                }
            }
            else if (StringToFormat.Length == 13)
            {
                int year = int.Parse(StringToFormat.Substring(0, 4));
                int month = int.Parse(StringToFormat.Substring(4, 2));
                int day = int.Parse(StringToFormat.Substring(6, 2));
                int hour = int.Parse(StringToFormat.Substring(9, 2));
                int minute = int.Parse(StringToFormat.Substring(11, 2));

                formattedString = month.ToString("00") + "/" + day.ToString("00") + "/" + year.ToString("0000")
                                + " " + hour.ToString("00") + ":" + minute.ToString("00");
            }

            return formattedString;
        }

        public int SegmentNumber
        {
            get
            {
                return segmentNumber;
            }
            set
            {
                segmentNumber = value;
            }
        }

        public int NumElements
        {
            get
            {
                int numElements = 0;

                if (!string.IsNullOrEmpty(SegmentString))
                {
                    numElements = SegmentString.Split(this.ElementSeperator.ToCharArray()).Count();
                }

                return numElements;
            }
        }

        public int GetNumComponents(int index)
        {
            int numComp = 0;

            if (NumElements > index)
            {
                numComp = this[index].Split(this.ComponentSeperator.ToCharArray()).Count();
            }

            return numComp;
        }

        public string this[int index]
        {
            get
            {
                string retElement = null;

                string[] elements = SegmentString.Split(this.ElementSeperator.ToCharArray());

                if (index < elements.Count())
                {
                    retElement = elements[index];
                }
                else
                {
                    if (InvalidElementIndex != null)
                    {
                        InvalidElementIndex(this, new EventArgs());
                    }
                }

                return retElement;
            }
        }

        public string this[int index, int? subIndex]
        {
            get
            {
                string retValue = null;

                if (subIndex.HasValue)
                {
                    string element = this[index];
                    string[] components = element.Split(this.ComponentSeperator.ToCharArray());

                    if ((subIndex.Value - 1) < components.Count())
                    {
                        retValue = components[subIndex.Value - 1];
                    }
                    else
                    {
                        if (InvalidComponentIndex != null)
                        {
                            InvalidComponentIndex(this, new EventArgs());
                        }
                    }

                }
                else
                {
                    retValue = this[index];
                }

                return retValue;
            }
        }
    }

    public partial class X12FileStructure
    {
		public class SegmentStructure
		{
			public string UniqueID = null;
			public string SegmentID = null;
			public bool Required = false;
			public List<string> QualifierValueList = null;
			public string StringMax = null;
			public string QualifierElement = null;

			public int Max
			{
				get
				{
					int tempMax = 0;

					if (!string.IsNullOrEmpty(StringMax))
					{
						string tempStringMax = StringMax;
						if (tempStringMax.Substring(0, 1) == ">")
						{
							tempStringMax = tempStringMax.Substring(1);
						}

						try
						{
							tempMax = int.Parse(tempStringMax);
						}
						catch { }
					}
					return tempMax;
				}
			}
		}

		public List<X12FileStructure> ChildLoops = new List<X12FileStructure>();

        public string ID;
        public string LoopName;
        public X12FileStructure Parent ;
        public X12FileStructure NextSibiling;
     
        public List<SegmentStructure> Segments = new List<SegmentStructure>();       

        public static X12FileStructure BuildFileStructure(XElement xmlParsed)
        {
            X12FileStructure fileStruct = new X12FileStructure();
            List<string> errorList = new List<string>();

            bool success = true;

            if (success)
            {
                fileStruct.LoopName = "FILE";
                fileStruct.ChildLoops = X12FileStructure.GetChildernForLoop(xmlParsed, fileStruct, null, null);
            }
            return fileStruct;
        }

        private static List<X12FileStructure> GetChildernForLoop(XElement xmlFileStruct, X12FileStructure LoopParent, string ID, string ParentLoop)
        {
            List<X12FileStructure> list = new List<X12FileStructure>();

            var childLoops = (from child in xmlFileStruct.Descendants("X12Element")
                              where ((child.Attributes("ParentSegment").Any() == true)
                                      && child.Attribute("ParentSegment").Value.ToUpper() == "ME"
                                    )
                                 && ((string.IsNullOrEmpty(ParentLoop)
                                        && ((child.Attributes("ParentLoop").Any() == false)
                                            || string.IsNullOrEmpty(child.Attribute("ParentLoop").Value)
                                            || child.Attribute("ParentLoop").Value == "."
                                           )
                                      )
                                      || ((!string.IsNullOrEmpty(ParentLoop))
                                            && (child.Attributes("ParentLoop").Any() == true)
                                            && child.Attribute("ParentLoop").Value.ToUpper() == ParentLoop.ToUpper()
                                         )
                                    )
                              orderby int.Parse(child.Attribute("UniqueID").Value)
                              select new
                              {
                                  Name = child.Attribute("Loop").Value.ToUpper()
                              }
                            ).Distinct();

            if (childLoops != null)
            {
                X12FileStructure last = null;
                foreach (var child in childLoops)
                {
                    X12FileStructure fileStruct = new X12FileStructure();
                    if (last != null)
                    {
                        last.NextSibiling = fileStruct;
                    }

                    fileStruct.Parent = LoopParent;

                    if (string.IsNullOrEmpty(child.Name))
                    {
                        fileStruct.LoopName = "FILE";
                    }
                    else
                    {
                        fileStruct.LoopName = child.Name.ToUpper();
                        fileStruct.Segments = (from seg in xmlFileStruct.Descendants("X12Element")
                                               where seg.Attribute("Loop").Value.ToUpper() == fileStruct.LoopName
                                               orderby int.Parse(seg.Attribute("UniqueID").Value)
                                               select new SegmentStructure
                                               {
                                                   UniqueID = seg.Attribute("UniqueID").Value.ToUpper(),
                                                   SegmentID = seg.Attribute("ElementId").Value.ToUpper(),
                                                   Required = (seg.Attributes("UsageReq").Any() == true)
                                                                 ? seg.Attribute("UsageReq").Value.ToUpper() == "R"
                                                                 : false,
                                                   StringMax = seg.Attribute("MinMax").Value.ToUpper()
                                               }
                                              ).Distinct().ToList();

                        foreach (SegmentStructure segStr in fileStruct.Segments)
                        {
                            var qual = (from seg in xmlFileStruct.Descendants("X12Element")
                                        where seg.Attribute("ParentSegment").Value == segStr.UniqueID
                                          && ((seg.Attributes("Qualifier").Any() == true)
                                               && !string.IsNullOrEmpty(seg.Attribute("Qualifier").Value)
                                               && (seg.Attribute("Qualifier").Value != ".")
                                             )
                                        orderby int.Parse(seg.Attribute("UniqueID").Value)
                                        select new
                                        {
                                            Qualifier = seg.Attribute("Qualifier").Value,
                                            ElementID = seg.Attribute("ElementId").Value
                                        }
                                       );

                            if (qual.Any() == true)
                            {
                                segStr.QualifierElement = qual.First().ElementID;
                                segStr.QualifierValueList = qual.First().Qualifier.Split(';').ToList();
                            }
                        }
                    }

                    fileStruct.ChildLoops = X12FileStructure.GetChildernForLoop(xmlFileStruct, fileStruct, fileStruct.LoopName, child.Name);
                    list.Add(fileStruct);
                    last = fileStruct;
                }
            }

            return list;
        }

        public SegmentStructure GetSegmentFromLoop(string SegmentString, char? ElementSeperator, char? ComponentSeperator)
        {
            SegmentStructure segmentStruct = null;

            string segmentID = SegmentString;

            if (segmentID.Contains(ElementSeperator.Value.ToString()))
            {
                segmentID = segmentID.Substring(0, segmentID.IndexOf((char)ElementSeperator));
            }

            foreach (SegmentStructure segStruct in Segments)
            {
                if (segStruct.SegmentID == segmentID)
                {
                    if (string.IsNullOrEmpty(segStruct.QualifierElement))
                    {
                        segmentStruct = segStruct;
                        break;
                    }
                    else
                    {
                        try
                        {
                            string qualElem = segStruct.QualifierElement;
                            string qualSeg = null;

                            if (qualElem.Contains("-"))
                            {
                                qualSeg = qualElem.Split('-')[1];
                                qualElem = qualElem.Split('-')[0];
                            }

                            int i = int.Parse(qualElem.Substring(qualElem.Length - 2));
                            if (string.IsNullOrEmpty(qualSeg))
                            {
                                if (segStruct.QualifierValueList.Contains(SegmentString.Split(ElementSeperator.Value)[i]))
                                {
                                    segmentStruct = segStruct;
                                    break;
                                }
                            }
                            else
                            {
                                string element = SegmentString.Split(ElementSeperator.Value)[i];
                                int segIndex = int.Parse(qualSeg);
                                if (segStruct.QualifierValueList.Contains(element.Split(ComponentSeperator.Value)[segIndex - 1]))
                                {
                                    segmentStruct = segStruct;
                                    break;
                                }
                            }
                        }
                        catch
                        {
                        }
                    }
                }

            }

            return segmentStruct;
        }

        public X12FileStructure GetLoopByName(string LoopName)
        {
            if (this.LoopName == LoopName)
            {
                return this;
            }

            foreach (X12FileStructure x in this.ChildLoops)
            {
                X12FileStructure retStruct = x.GetLoopByName(LoopName);

                if (retStruct != null)
                {
                    return retStruct;
                }
            }

            return null;
        }

    }

    public partial class InsuranceParameterHelpers
    {
        public static bool GetPayerParameter(IEnumerable<InsuranceParameterDefinition> DefinitionList, IEnumerable<InsuranceParameter> ParmList,
                                         string MethodID, bool HardError, ref string RetString, List<string> ErrorList)
        {
            bool success = true;
            RetString = null;

            var value = from def in DefinitionList
                        join parm in ParmList
                          on def.InsuranceParameterDefinitionKey equals parm.ParameterKey
                        where def.Code.ToUpper() == MethodID.ToUpper()
                        select parm.Value;

            if (value.Any() == true)
            {
                RetString = value.First();
            }
            else
            {
                var defValue = DefinitionList.Where(d => d.Code.ToUpper() == MethodID.ToUpper()).FirstOrDefault();
                if (defValue != null)
                {
                    RetString = defValue.DefaultValue;
                }
            }

            if (HardError
                && string.IsNullOrEmpty(RetString)
               )
            {
                success = false;
                ErrorList.Add("Missing EbillPayerParam row : " + MethodID);
            }

            return success;
        }

        public static bool RequiredParametersExist(IEnumerable<InsuranceParameterDefinition> DefinitionList, IEnumerable<InsuranceParameter> ParmList, int InsuranceKey,
                                                  string ParameterType, string FileID, List<string> ErrorList)
        {
            bool success = true;
            List<string> requiredParameters = new List<string>();
            string value = null;

            if (FileID == "270")
            {
                requiredParameters.Add("270Group");
                requiredParameters.Add("RCVRNAME");
                requiredParameters.Add("IDENTQUAL");
                requiredParameters.Add("270RCVID");
                requiredParameters.Add("INTCONNUM");
                requiredParameters.Add("GROUPCONT");
            }

            requiredParameters.ForEach(s =>
            {
                bool mySuccess = true;
                mySuccess = GetPayerParameter(DefinitionList, ParmList, s, true, ref value, ErrorList);
                success = success && mySuccess;
            }
            );
            return success;
        }

        public static bool IncrementEBillPayerParam(IEnumerable<InsuranceParameterDefinition> DefinitionList, IEnumerable<InsuranceParameter> ParmList, int InsuranceKey,
                                                          string ParameterType, string MethodID, List<string> ErrorList)
        {
            bool success = true;
            Int32 IntValue = 0;

            var value = from def in DefinitionList
                        join parm in ParmList
                          on def.InsuranceParameterDefinitionKey equals parm.ParameterKey
                        where def.Code.ToUpper() == MethodID.ToUpper()
                        select parm;

            if (success)
            {
                try
                {
                    if (value.Any() == true)
                    {
                        IntValue = Convert.ToInt32(value.First().Value);
                        IntValue++;
                        value.First().Value = IntValue.ToString();
                    }
                    else
                    {
                        success = false;
                        ErrorList.Add("Insurance Parameter '" + MethodID + "' does not exits");
                    }
                }
                catch (Exception e)
                {
                    success = false;
                    ErrorList.Add("Invalid " + MethodID + " : " + e.Message);
                    //throw new Exception("Invalid " + SearchStr + " : " + e.Message);
                }
            }
            return success;
        }

    }

    public class Segment
    {
		private string value = null;
		public List<Element> Elements = new List<Element>();
		
		public string Value
        {
            get
            {
                return value.ToUpper();
            }
            set
            {
                this.value = value;
            }
        }
    }

    public class Element
    {
		public string Value = null;
		public List<Component> Components = null;
    }

    public class Component
    {
		public string Value = null;
    }
}

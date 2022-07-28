using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using ErrList;
using System.IO;
using System.Xml.Linq;
using System.Reflection;
//using Excel = Microsoft.Office.Interop.Excel; 

namespace Virtuoso.Server.Data
{
	public class X12
	{
		public readonly List<string> Errors;
		public readonly string xmlFileName;
		public readonly string xmlAsString;
		public readonly string dtp012110C;
		public readonly string dtp01;
		public readonly string eb03;
		public readonly string eb09;
		public readonly string eb13_1;

		public readonly string aaa03_2100C;
		public readonly string aaa04_2100C;

		public readonly string aaa03_2100D;
		public readonly string aaa04_2100D;
		public readonly string eb01;
		public readonly string eb02;
		public readonly string eb04;
		public readonly string eb06;
		public readonly string eb11;
		public readonly string eb12;

		public X12()
		{
			List<string> errors = new List<string>();
			XElement xmlParsed = null;
			xmlFileName = GenericReadX12.GetXMLFileName(GenericReadX12.X12FileType.X12_5010_0271);
			GenericReadX12.ReadEmbeddedXMLScript("Virtuoso.Server.Data.Utility.Assets", xmlFileName, ref xmlParsed, ref errors);
			xmlAsString = (xmlParsed == null) ? null : xmlParsed.ToString();
			GenericReadX12.ReadEmbeddedXMLScript("Virtuoso.Server.Data.Utility.Assets", "2110CDTP01.xml", ref xmlParsed, ref errors);
			dtp012110C = (xmlParsed == null) ? null : xmlParsed.ToString();
			GenericReadX12.ReadEmbeddedXMLScript("Virtuoso.Server.Data.Utility.Assets", "DTP01.xml", ref xmlParsed, ref errors);
			dtp01 = (xmlParsed == null) ? null : xmlParsed.ToString();
			GenericReadX12.ReadEmbeddedXMLScript("Virtuoso.Server.Data.Utility.Assets", "EB03.xml", ref xmlParsed, ref errors);
			eb03 = (xmlParsed == null) ? null : xmlParsed.ToString();
			GenericReadX12.ReadEmbeddedXMLScript("Virtuoso.Server.Data.Utility.Assets", "EB09.xml", ref xmlParsed, ref errors);
			eb09 = (xmlParsed == null) ? null : xmlParsed.ToString();
			GenericReadX12.ReadEmbeddedXMLScript("Virtuoso.Server.Data.Utility.Assets", "EB13-1.xml", ref xmlParsed, ref errors);
			eb13_1 = (xmlParsed == null) ? null : xmlParsed.ToString();

			GenericReadX12.ReadEmbeddedXMLScript("Virtuoso.Server.Data.Utility.Assets", "2100C AAA03.xml", ref xmlParsed, ref errors);
			aaa03_2100C = (xmlParsed == null) ? null : xmlParsed.ToString();
			GenericReadX12.ReadEmbeddedXMLScript("Virtuoso.Server.Data.Utility.Assets", "2100C AAA04.xml", ref xmlParsed, ref errors);
			aaa04_2100C = (xmlParsed == null) ? null : xmlParsed.ToString();

			GenericReadX12.ReadEmbeddedXMLScript("Virtuoso.Server.Data.Utility.Assets", "2100C AAA03.xml", ref xmlParsed, ref errors);
			aaa03_2100D = (xmlParsed == null) ? null : xmlParsed.ToString();
			GenericReadX12.ReadEmbeddedXMLScript("Virtuoso.Server.Data.Utility.Assets", "2100C AAA04.xml", ref xmlParsed, ref errors);
			aaa04_2100D = (xmlParsed == null) ? null : xmlParsed.ToString();

			GenericReadX12.ReadEmbeddedXMLScript("Virtuoso.Server.Data.Utility.Assets", "EB01.xml", ref xmlParsed, ref errors);
			eb01 = (xmlParsed == null) ? null : xmlParsed.ToString();

			GenericReadX12.ReadEmbeddedXMLScript("Virtuoso.Server.Data.Utility.Assets", "EB02.xml", ref xmlParsed, ref errors);
			eb02 = (xmlParsed == null) ? null : xmlParsed.ToString();

			GenericReadX12.ReadEmbeddedXMLScript("Virtuoso.Server.Data.Utility.Assets", "EB04.xml", ref xmlParsed, ref errors);
			eb04 = (xmlParsed == null) ? null : xmlParsed.ToString();

			GenericReadX12.ReadEmbeddedXMLScript("Virtuoso.Server.Data.Utility.Assets", "EB06.xml", ref xmlParsed, ref errors);
			eb06 = (xmlParsed == null) ? null : xmlParsed.ToString();

			GenericReadX12.ReadEmbeddedXMLScript("Virtuoso.Server.Data.Utility.Assets", "EB11.xml", ref xmlParsed, ref errors);
			eb11 = (xmlParsed == null) ? null : xmlParsed.ToString();

			GenericReadX12.ReadEmbeddedXMLScript("Virtuoso.Server.Data.Utility.Assets", "EB12.xml", ref xmlParsed, ref errors);
			eb12 = (xmlParsed == null) ? null : xmlParsed.ToString();

			Errors = errors;
		}
	}

    public partial class GenericReadX12
    {
        public enum X12FileType
        {
            Unknown,
            X12_999,
            X12_277CA,
            X12_837I,
            X12_U277,
            X12_5010_0271
        }

        protected int EnvelopeLength = 106;
        protected int SegSeperatorPosition = 105;
        protected int ElemSeperatorPosition = 103;
        protected int CompSeperatorPosition = 104;
        protected int RepSeperatorPosition = 82;
		private X12File x12File = new X12File();

		private char? segmentSeperator = null;
		private char? repetitionSeperator = null;
		private char? elementSeperator = null;
		public char? componentSeperator = null;
		private string fileData = null;
			
		public List<Segment> SegmentList = null;
		protected List<string> gs08 = new List<string>();

		public GenericReadX12()
		{
			gs08.Add("005010X231");
			gs08.Add("005010X231A1");
		}

        public X12File X12File
        {
            get
            {
                return x12File;
            }
            set
            {
                x12File = value;
            }
        }

        public char SegmentSeperator
        {
            get
            {
                if (segmentSeperator == null)
                {
                    if (!string.IsNullOrEmpty(fileData))
                    {
                        if (SegSeperatorPosition < fileData.Length)
                        {
                            segmentSeperator = fileData.Substring(SegSeperatorPosition, 1).ToCharArray()[0];
                        }
                    }
                }
				if (segmentSeperator == null)
				{
					segmentSeperator = "~".ToCharArray()[0];
				}
				return segmentSeperator.Value;               
            }
        }

        public char RepetitionSeperator
        {
            get
            {
				if (repetitionSeperator == null)
                {
                    if (!string.IsNullOrEmpty(fileData))
                    {
                        if (RepSeperatorPosition < fileData.Length)
                        {
							repetitionSeperator = fileData.Substring(RepSeperatorPosition, 1).ToCharArray()[0];
                        }
                    }
                }
				if (repetitionSeperator == null)
				{
					repetitionSeperator = "}".ToCharArray()[0];
				}
				return repetitionSeperator.Value;
            }
        }
        //private char? repetitionSeperator = null;

        public char ElementSeperator
        {
            get
            {              
                if (elementSeperator == null)
                {
                    if (!string.IsNullOrEmpty(fileData))
                    {
                        if (ElemSeperatorPosition < fileData.Length)
                        {
                            elementSeperator = fileData.Substring(ElemSeperatorPosition, 1).ToCharArray()[0];
                        }
                    }
                }

				if (elementSeperator == null)
				{
					elementSeperator = "|".ToCharArray()[0] ;
				}
                return elementSeperator.Value;
            }
        }

        public char ComponentSeperator
        {
			get
			{
				if (componentSeperator == null)
				{
					if (!string.IsNullOrEmpty(fileData))
					{
						if (CompSeperatorPosition < fileData.Length)
						{
							componentSeperator = fileData.Substring(CompSeperatorPosition, 1).ToCharArray()[0];
						}
					}
				}
				if (componentSeperator == null)
				{
					componentSeperator = "^".ToCharArray()[0];
				}
				return componentSeperator.Value;
			}
			set
			{
				componentSeperator = value;
			}
        }

        public bool GetTypeOfFile(string filedata, ref X12FileType fileType, List<string> errorList)
        {
            bool success = true;
            try
            {
				this.fileData = filedata;
				this.segmentSeperator = null;
				this.elementSeperator = null;

				string strData = string.IsNullOrEmpty(filedata) ? null : filedata; //.Substring(0, 400);

				// remove the carrage returns and line feeds and split
				string[] segments = strData.Replace("\r", string.Empty)
											.Replace("\n", string.Empty)
											.Split(this.SegmentSeperator)
											.Select(s => s.Trim())
											.Where(w => w.Length > 0)
											.ToArray();
             
                string[] elements = null;
                string[] gsElements = null;

                if (segments.Any() == true)
                {
					elements = segments[0].Split(this.ElementSeperator).Select(s => s.Trim()).ToArray();

                    if (elements.Any() == true)
                    {
                        if (elements[0].ToUpper() == "ISA")
                        {
                            if (segments.Count() > 1)
                            {
								elements = segments[1].Split(this.ElementSeperator).Select(s => s.Trim()).ToArray();

                                if (elements.Any() == true)
                                {
                                    if (elements[0] == "GS")
                                    {
                                        gsElements = elements;
                                    }
                                    else if (elements[0] == "TA1")
                                    {
                                        if (segments.Count() > 2)
                                        {
											elements = segments[2].Split(this.ElementSeperator).Select(s => s.Trim()).ToArray();
                                            if (elements[0] == "GS")
                                            {
                                                gsElements = elements;
                                            }
                                        }
                                    }
                                }
                            }

                            if (gsElements != null)
                            {
                                if (gsElements.Count() > 8)
                                {
                                    if (gsElements[8].ToUpper() == "005010X214")
                                    {
                                        fileType = X12FileType.X12_277CA;
                                    }
                                    else if (gs08.Contains(gsElements[8].ToUpper()))
                                    {
                                        fileType = X12FileType.X12_999;
                                    }
                                    else if (gsElements[8].ToUpper() == "837V4010")
                                    {
                                        fileType = X12FileType.X12_837I;
                                    }
                                    else if (gsElements[8].ToUpper() == "004010X093A1")
                                    {
                                        fileType = X12FileType.X12_U277;
                                    }
                                    else if (gsElements[8].ToUpper() == "005010X279A1")
                                    {
                                        fileType = X12FileType.X12_5010_0271;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                success = false;
                errorList.Add(e.Message);
            }
            return success;
        }

        public static string GetXMLFileName(X12FileType FileType)
        {
            string fileName = null;

            if (FileType == X12FileType.X12_277CA)
            {
                fileName = "277CALayout.xml";
            }
            if (FileType == X12FileType.X12_837I)
            {
                fileName = "837iLayout.xml";
            }
            if (FileType == X12FileType.X12_U277)
            {
                fileName = "U277Layout.xml";
            }
            if (FileType == X12FileType.X12_5010_0271)
            {
                fileName = "5010-271Layout.xml";
            }

            return fileName;
        }

        public bool Read(string dataFile, X12FileType FileType, List<string> errorList)
        {
            bool success = true;

	
          
			this.fileData = dataFile;
			
			// Clear Values (will be reset from file)
			segmentSeperator = null;
			elementSeperator = null;
			repetitionSeperator = null;
			componentSeperator = null; 

            try
            {
                if (success)
                {
                    Read(dataFile, null, FileType, null, null, null, null, null, errorList);
                }
            }
            catch (Exception e)
            {
                success = false;
                errorList.Add(e.Message);
            }

            return success;
        }

		public bool Read(string data, string startingLoop, X12FileType FileType, string xmlAsString,
						 string SegmentSeparatorOvr, string ElementSeparatorOvr, string ComponentSeparatorOvr, string RepetitionSeperatorOvr,
						 List<string> errorList)
		{
			bool success = true;

			string xmlFileName = GetXMLFileName(FileType);
			XElement xmlParsed = null;
			if (string.IsNullOrEmpty(xmlAsString))
			{
				success = GenericReadX12.ReadEmbeddedXMLScript("Virtuoso.Server.Data.Utility.Assets", xmlFileName, ref xmlParsed, ref errorList);
			}
			else
			{
				xmlParsed = XElement.Parse(xmlAsString);
			}
			X12FileStructure x12FileStructure = X12FileStructure.BuildFileStructure(xmlParsed);
			X12FileStructure x12StructCurr = null;
			Loop currLoop = null;

			fileData = data;
			segmentSeperator = ((string.IsNullOrEmpty(SegmentSeparatorOvr)) ? null : (SegmentSeparatorOvr.ToCharArray()[0] as char?));
			elementSeperator = ((string.IsNullOrEmpty(ElementSeparatorOvr)) ? null : (ElementSeparatorOvr.ToCharArray()[0] as char?));
			componentSeperator = ((string.IsNullOrEmpty(ComponentSeparatorOvr)) ? null : (ComponentSeparatorOvr.ToCharArray()[0] as char?));
			repetitionSeperator = ((string.IsNullOrEmpty(RepetitionSeperatorOvr)) ? null : (RepetitionSeperatorOvr.ToCharArray()[0] as char?));

			if (!string.IsNullOrEmpty(startingLoop))
			{
				x12StructCurr = x12FileStructure.GetLoopByName(startingLoop);
			}

			if (string.IsNullOrEmpty(xmlFileName))
			{
				success = false;
				errorList.Add("Unsupported file type");
			}
			else
			{			
				x12File.Loops.Clear();
				if (x12StructCurr == null)
				{
					x12StructCurr = x12FileStructure;
				}
				X12FileStructure.SegmentStructure currSegment = null;
				int segmentNumber = 0;

				var segs = fileData	.Replace("\r", string.Empty)
									.Replace("\n", string.Empty)
									.Split(this.SegmentSeperator)
									.Select(s => s.Trim())
									.Where(w => w.Length > 0)
									.ToArray();

				foreach (var s in segs)
				{
					try
					{
						segmentNumber++;
						bool found = false;

						currSegment = x12StructCurr.GetSegmentFromLoop(s, ElementSeperator, ComponentSeperator);
						if (currSegment != null)
						{
							X12Segment newSeg = new X12Segment(ElementSeperator, ComponentSeperator, RepetitionSeperator, segmentNumber);
							newSeg.UniqueID = currSegment.UniqueID;
							newSeg.SegmentID = currSegment.SegmentID;
							newSeg.SegmentString = s;
							if (currLoop == null)
							{
								currLoop = new Loop();
								currLoop.Name = x12StructCurr.LoopName;
								currLoop.SegmentSeparator = this.SegmentSeperator.ToString();
								currLoop.Parent = null;
								currLoop.Segments.Add(newSeg);
								x12File.Loops.Add(currLoop);
							}
							else if (!currLoop.ContainsMaxNumSegments(currSegment))
							{
								currLoop.Segments.Add(newSeg);
							}
							else
							{
								Loop newLoop = new Loop();
								newLoop.Name = currLoop.Name;
								newLoop.SegmentSeparator = this.SegmentSeperator.ToString();
								newLoop.Parent = currLoop.Parent;
								newLoop.Segments.Add(newSeg);
								if (newLoop.Parent == null)
								{
									if (x12File.Loops.Any() == true)
									{
										x12File.Loops.Last().NextSibling = newLoop;
									}
									x12File.Loops.Add(newLoop);
								}
								else
								{
									if (newLoop.Parent.Loops.Any() == true)
									{
										newLoop.Parent.Loops.Last().NextSibling = newLoop;
									}
									newLoop.Parent.Loops.Add(newLoop);
								}
								currLoop = newLoop;
							}
							found = true;
						}
						else
						{
							// check to see if the segment is in one of the child loops
							foreach (X12FileStructure child in x12StructCurr.ChildLoops)
							{
								currSegment = child.GetSegmentFromLoop(s, ElementSeperator, ComponentSeperator);
								if (currSegment != null)
								{
									found = true;
									x12StructCurr = child;
									Loop newLoop = new Loop();
									X12Segment newSeg = new X12Segment(ElementSeperator, ComponentSeperator, RepetitionSeperator, segmentNumber);
									newSeg.UniqueID = currSegment.UniqueID;
									newSeg.SegmentID = currSegment.SegmentID;
									newSeg.SegmentString = s;
									newLoop.Name = child.LoopName;
									newLoop.SegmentSeparator = this.SegmentSeperator.ToString();
									newLoop.Segments.Add(newSeg);

									if (currLoop == null)
									{
										if (x12File.Loops.Any() == true)
										{
											x12File.Loops.Last().NextSibling = newLoop;
										}
										x12File.Loops.Add(newLoop);
									}
									else
									{
										if (currLoop.Loops.Any() == true)
										{
											currLoop.Loops.Last().NextSibling = newLoop;
										}
										currLoop.Loops.Add(newLoop);
										newLoop.Parent = currLoop;
									}

									currLoop = newLoop;
									break;
								}
							}

							if (!found)
							{
								// search through NextSibilings of the current loop
								X12FileStructure temp = x12StructCurr.NextSibiling;
								while (temp != null)
								{
									currSegment = temp.GetSegmentFromLoop(s, ElementSeperator, ComponentSeperator);
									if (currSegment != null)
									{
										found = true;
										x12StructCurr = temp;
										break;
									}
									else
									{
										temp = temp.NextSibiling;
									}
								}

								if (found)
								{
									Loop newLoop = new Loop();
									X12Segment newSeg = new X12Segment(ElementSeperator, ComponentSeperator, RepetitionSeperator, segmentNumber);
									newSeg.UniqueID = currSegment.UniqueID;
									newSeg.SegmentID = currSegment.SegmentID;
									newSeg.SegmentString = s;
									newLoop.Name = temp.LoopName;
									newLoop.SegmentSeparator = this.SegmentSeperator.ToString();
									newLoop.Segments.Add(newSeg);
									newLoop.Parent = currLoop.Parent;
									if (currLoop.Parent.Loops.Any() == true)
									{
										currLoop.Parent.Loops.Last().NextSibling = newLoop;
									}
									currLoop.Parent.Loops.Add(newLoop);
									currLoop = newLoop;
								}
							}

							if ((!found) && (currLoop != null))
							{
								// search through ancestors of the current loop.  first check to see if the segment is in the parent.
								// if it is not in the parent loop, search through the parent's next siblings.  if the loop still is 
								// not found, go to the parent's parent and the parent's parent's next siblings and keep going untill
								// the loop is found or we run out of parents.
								X12FileStructure parent = x12StructCurr.Parent;
								Loop tempLoop = currLoop;
								while (parent != null)
								{
									tempLoop = tempLoop.Parent;
									currSegment = parent.GetSegmentFromLoop(s, ElementSeperator, ComponentSeperator);
									if (currSegment != null)
									{
										found = true;
										currLoop = tempLoop;
										x12StructCurr = parent;
										X12Segment newSeg = new X12Segment(ElementSeperator, ComponentSeperator, RepetitionSeperator, segmentNumber);
										newSeg.UniqueID = currSegment.UniqueID;
										newSeg.SegmentID = currSegment.SegmentID;
										newSeg.SegmentString = s;
										if (!currLoop.ContainsMaxNumSegments(currSegment))
										{
											currLoop.Segments.Add(newSeg);
										}
										else
										{
											Loop newLoop = new Loop();
											newLoop.Name = currLoop.Name;
											newLoop.Parent = currLoop.Parent;
											newLoop.SegmentSeparator = this.SegmentSeperator.ToString();
											newLoop.Segments.Add(newSeg);
											if (newLoop.Parent.Loops.Any() == true)
											{
												newLoop.Parent.Loops.Last().NextSibling = newLoop;
											}
											newLoop.Parent.Loops.Add(newLoop);
											currLoop = newLoop;
										}

										break;
									}
									else
									{
										// search through parents NextSibilings
										X12FileStructure temp = parent.NextSibiling;
										while (temp != null)
										{
											currSegment = temp.GetSegmentFromLoop(s, ElementSeperator, ComponentSeperator);
											if (currSegment != null)
											{
												found = true;
												x12StructCurr = temp;

												Loop newLoop = new Loop();
												X12Segment newSeg = new X12Segment(ElementSeperator, ComponentSeperator, RepetitionSeperator, segmentNumber);
												newSeg.UniqueID = currSegment.UniqueID;
												newSeg.SegmentID = currSegment.SegmentID;
												newSeg.SegmentString = s;
												newLoop.Name = temp.LoopName;
												newLoop.SegmentSeparator = this.SegmentSeperator.ToString();
												newLoop.Segments.Add(newSeg);
												if (tempLoop.Parent.Loops.Any() == true)
												{
													tempLoop.Parent.Loops.Last().NextSibling = newLoop;
												}
												tempLoop.Parent.Loops.Add(newLoop);
												newLoop.Parent = tempLoop.Parent;
												currLoop = newLoop;

												break;
											}
											else
											{
												temp = temp.NextSibiling;
											}
										}

										if (found)
										{
											break;
										}
										else
										{
											parent = parent.Parent;
										}
									}
								}
							}

						}

						if (found)
						{
							currLoop.Segments[currLoop.Segments.Count - 1].KnownSegment = true;
						}
						else
						{
							if (!string.IsNullOrEmpty(s) && (currLoop != null))
							{
								// segment wasn't found
								X12Segment newSeg = new X12Segment(ElementSeperator, ComponentSeperator, RepetitionSeperator, segmentNumber);
								newSeg.KnownSegment = false;
								newSeg.SegmentID = s.Split(SegmentSeperator)[0];
								newSeg.SegmentString = s;
								currLoop.Segments.Add(newSeg);
							}
						}
					}
					catch (Exception e)
					{
						var e2 = new Exception("Error Parsing 271. [" + segmentNumber.ToString() + "] " + s + "[" + e.Message + "]", e);
						throw e2;
					}
				}

			}

			return success;
		}

        public bool IsFileDataValid(List<string> errorList)
        {
            bool isValid = true;
            Segment gsSegment = null;

            try
            {
                if (fileData.Length < EnvelopeLength)
                {
                    isValid = false;
                    errorList.Add("Invalid File" + Environment.NewLine + "Cannot determine seperators");
                }

                if (fileData.Substring(0, 3).ToUpper() != "ISA")
                {
                    isValid = false;
                    errorList.Add("Invalid File" + Environment.NewLine + "The file does not start with an ISA segment");
                }

                if (isValid)
                {
                    Segment tempSeg = SegmentList[1];

                    if (tempSeg.Elements[0].Value == "GS")
                    {
                        gsSegment = tempSeg;
                    }
                    else if ((tempSeg.Elements[0].Value == "TA1")
                            && (SegmentList[2].Elements[0].Value == "GS")
                           )
                    {
                        gsSegment = SegmentList[2];
                    }

                    if (gsSegment == null)
                    {
                        isValid = false;
                        errorList.Add("Invalid File" + Environment.NewLine + "Could not determine GS segment.");
                    }
                }

                if (isValid)
                {
                    if (gsSegment.Elements.Count < 8)
                    {
                        isValid = false;
                        errorList.Add("Invalid File" + Environment.NewLine + "Element GS08 does not exist");
                    }
                    else
                    {
                        if (!gs08.Contains(gsSegment.Elements[08].Value))
                        {
                            isValid = false;
                            string or = "";
                            string errMessage = "Invalid File" + Environment.NewLine + "Element GS08 is \"" + gsSegment.Elements[08].Value + "\""
                                                + " Valid 999 files have a GS08 of \"";

                            foreach (string s in gs08)
                            {
                                errMessage += or + s + "\"";
                                or += " or \"";
                            }

                            errorList.Add(errMessage);
                        }
                    }
                }
            }
            catch
            {
                isValid = false;
                errorList.Add("Invalid File" + Environment.NewLine + "Improper file structure");
            }

            return isValid;
        }

        public static bool ReadEmbeddedXMLScript(string NameSpace, string FileName, ref XElement ParsedXML, ref List<string> errorList)
        {
            bool success = true;

           try
            {
               ParsedXML = null;
               Assembly a = Assembly.GetExecutingAssembly();

               var x = a.GetManifestResourceNames();

               Stream s = a.GetManifestResourceStream(NameSpace + "." + FileName);

                string strCodes = null;

                using (StreamReader sr = new StreamReader(s, false))
                {
                    strCodes = sr.ReadToEnd();
                }

                if (success)
                {
                    ParsedXML = XElement.Parse(strCodes);
                }
            }
            catch (Exception ex)
            {
                success = false;
                errorList.Add(ex.Message);
            }
            return success;
        }

        public List<OtherInsuranceInformation> GetOtherInsuranceInformation(List<Loop> Loop2000CList)
        {
            List<OtherInsuranceInformation> otherInsList = new List<OtherInsuranceInformation>();

            foreach (Loop loop in Loop2000CList)
            {
                int insuranceVerificationRequestKey = 0;

				var trnSeg = loop.Segments.Where(segment => segment.SegmentID.ToUpper() == "TRN").ToList();
                foreach (X12Segment seg in trnSeg)
                {
                    if (seg.NumElements > 2)
                    {
                        if (int.TryParse(seg[2], out insuranceVerificationRequestKey))
                        {
                            var loop2120CList = loop.GetAllDescendantLoopsByName("2120C");
                            loop2120CList.ForEach(row =>
                            {
                                OtherInsuranceInformation otherIns = new OtherInsuranceInformation();
                                otherIns.InsuranceVerificationRequestKey = insuranceVerificationRequestKey;
                                Loop loop2110C = row.Parent;
                                if (loop2110C != null)
                                {
                                    X12Segment refSeg = loop2110C.Segments.Where(s => s.SegmentID.ToUpper() == "REF").FirstOrDefault();
                                    if ((refSeg != null)
                                        && (refSeg.NumElements > 2)
                                       )
                                    {
                                        otherIns.ReferenceIdentification = refSeg[2];
                                    }
                                }

                                var nm1Seg = row.Segments.Where(s => s.SegmentID.ToUpper() == "NM1").FirstOrDefault();

                                if ((nm1Seg != null)
                                    && (nm1Seg.NumElements > 3)
                                   )
                                {
                                    otherIns.InsuranceName = nm1Seg[3];
                                }

                                if ((nm1Seg != null)
                                    && (nm1Seg.NumElements > 9)
                                   )
                                {
                                    otherIns.InsuranceCode = nm1Seg[9];
                                }

                                var n3Seg = row.Segments.Where(s => s.SegmentID.ToUpper() == "N3").FirstOrDefault();

                                if ((n3Seg != null)
                                    && (n3Seg.NumElements > 1)
                                   )
                                {
                                    otherIns.Address1 = n3Seg[1];
                                }

                                var n4Seg = row.Segments.Where(s => s.SegmentID.ToUpper() == "N4").FirstOrDefault();

                                if ((n4Seg != null)
                                    && (n4Seg.NumElements > 1)
                                   )
                                {
                                    otherIns.City = n4Seg[1];
                                }

                                if ((n4Seg != null)
                                    && (n4Seg.NumElements > 2)
                                   )
                                {
                                    otherIns.StateCode = n4Seg[2];
                                }

                                if ((n4Seg != null)
                                    && (n4Seg.NumElements > 3)
                                   )
                                {
                                    otherIns.ZipCode = n4Seg[3];
                                }

                                var perSeg = row.Segments.Where(s => s.SegmentID.ToUpper() == "PER").FirstOrDefault();

                                if ((perSeg != null)
                                    && (perSeg.NumElements > 2)
                                   )
                                {
                                    otherIns.PayerContactName = perSeg[2];
                                }

                                if ((perSeg != null)
                                    && (perSeg.NumElements > 4)
                                   )
                                {
                                    otherIns.PayerContactNumber = perSeg[4];
                                }

                                otherInsList.Add(otherIns);
                            });
                        }
                    }
                }
            }

            return otherInsList;
        }
    }

    //public class ExcelReader
    //{
    //    public void ProcessExcel()
    //    {

    //    }

    //    public bool ReadExcel(ErrorList errorList)
    //    {
    //        bool success = true;

    //        Excel.Application xlApp;
    //        Excel.Workbook xlWorkBook;
    //        Excel.Worksheet xlWorkSheet;
    //        Excel.Range range;

    //        int rCnt = 0;

    //        xlApp = new Excel.ApplicationClass();
    //        xlWorkBook = xlApp.Workbooks.Open("C:\\Units\\DotNet\\999\\837I NFV Mapping Version 4.xls", 0, false, 5, "", "", true, Microsoft.Office.Interop.Excel.XlPlatform.xlWindows, "\t", false, false, 0, true, 1, 0);
    //        xlWorkSheet = (Excel.Worksheet)xlWorkBook.Worksheets.get_Item(3);

    //        // range = xlWorkSheet.get_Range("A1", "A2122");
    //        range = xlWorkSheet.UsedRange;
    //        string lastParent = null;
    //        for (rCnt = 1; rCnt <= range.Rows.Count; rCnt++)
    //        {
    //            if ((bool)(range.Cells[rCnt, 3] as Excel.Range).Font.Bold)
    //            {
    //                object o = (range.Cells[rCnt, 1] as Excel.Range).Value2;

    //                if ((o as double?) != null)
    //                {
    //                    lastParent = o.ToString();
    //                    (range.Cells[rCnt, 2] as Excel.Range).Value2 = "ME";
    //                }
    //            }
    //            else
    //            {
    //                (range.Cells[rCnt, 2] as Excel.Range).Value2 = lastParent;
    //            }
    //            //for (cCnt = 1; cCnt <= range.Columns.Count; cCnt++)
    //            //{
    //            //    object o = (range.Cells[rCnt, cCnt] as Excel.Range).Value2;

    //            //    if (o != null)
    //            //    {
    //            //        str = o.ToString();
    //            //    }
    //            //    else
    //            //    {
    //            //        str = "<NULL>";
    //            //    }
    //            //    errorList.Add(str);
    //            //    success = false;
    //            //}
    //        }

    //        xlWorkBook.Close(true, null, null);
    //        xlApp.Quit();

    //        success = success && releaseObject(xlWorkSheet, errorList);
    //        success = success && releaseObject(xlWorkBook, errorList);
    //        success = success && releaseObject(xlApp, errorList);
    //        return success;
    //    }

    //    private bool releaseObject(object obj, ErrorList errorList)
    //    {
    //        bool success = true;
    //        try
    //        {
    //            System.Runtime.InteropServices.Marshal.ReleaseComObject(obj);
    //            obj = null;
    //        }
    //        catch (Exception ex)
    //        {
    //            obj = null;
    //            errorList.Add(ex.Message);
    //            success = false;
    //        }
    //        finally
    //        {
    //            GC.Collect();
    //        }
    //        return success;
    //    } 
    //}
}

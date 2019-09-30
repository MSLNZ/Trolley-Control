using System;
using System.IO;
using System.Text;
using System.Data;
using System.Xml;
using System.Runtime.InteropServices;

namespace Trolley_Control
{
	/// <summary>
	/// WIN32 API Wrapper class
	/// </summary>
	public class WIN32Wrapper
	{
		/// <summary>
		/// Get all the section names from an INI file
		/// </summary>
		[ DllImport("kernel32.dll", EntryPoint="GetPrivateProfileSectionNamesA") ]
		public extern static int GetPrivateProfileSectionNames(
			[MarshalAs(UnmanagedType.LPArray)] byte[] lpReturnedString,
			int nSize,
			string lpFileName);

		/// <summary>
		/// Get all the settings from a section in a INI file
		/// </summary>
		[ DllImport("kernel32.dll", EntryPoint="GetPrivateProfileSectionA") ]
		public extern static int GetPrivateProfileSection(
			string lpAppName,
			[MarshalAs(UnmanagedType.LPArray)] byte[] lpReturnedString,
			int nSize,
			string lpFileName);
	}

	/// <summary>
	/// Convert an INI file into an XML file
	/// </summary>
	public class INI2XML
	{
		/// <summary>
		/// 
		/// </summary>
		public INI2XML()
		{
		}
		
		/// <summary>
		/// Initial size of the buffer used when calling the Win32 API functions
		/// </summary>
		const int INITIAL_BUFFER_SIZE = 4096;

		/// <summary>
		/// Converts an INI file into an XML file.
		/// Output XML file has the following structure...
		///   <?xml version="1.0"?>
		///   <configuration>
		///       <section name="Main">
		///           <setting name="Timeout" value="90"/>
		///           <setting name="Mode" value="Live"/>
		///      </section>
		///   </configuration>
		/// Example:
		///	if (Loki.INI2XML.Convert( txtIniFileName.Text ))
		///		Console.WriteLine( "Successfully converted \"" + txtIniFileName.Text + "\" to xml" );
		///	else
		///		Console.WriteLine( "Problem converting \"" + txtIniFileName.Text + "\" to xml" );
		/// If an exception is raised, it is passed on to the caller.
		/// </summary>
		/// <param name="strINIFileName">File name of the INI file to convert</param>
		/// <returns>True if successfuly, or False if a problem</returns>
		public static bool Convert( string strINIFileName )
		{
            string temp = "";
			return Convert(strINIFileName,ref temp);
		}

		/// <summary>
		/// Converts an INI file into an XML file.
		/// Output XML file has the following structure...
		///   <?xml version="1.0"?>
		///   <configuration>
		///       <section name="Main">
		///           <setting name="Timeout" value="90"/>
		///           <setting name="Mode" value="Live"/>
		///      </section>
		///   </configuration>
		/// Example:
		///	if (Loki.INI2XML.Convert( txtIniFileName.Text, txtXMLFileName.Text ))
		///		Console.WriteLine( "Successfully converted \"" + txtIniFileName.Text + "\" to \"" + txtXMLFileName.Text + "\"" );
		///	else
		///		Console.WriteLine( "Problem converting \"" + txtIniFileName.Text + "\" to \"" + txtXMLFileName.Text + "\"" );
		/// If an exception is raised, it is passed on to the caller.
		/// </summary>
		/// <param name="strINIFileName">File name of the INI file to convert</param>
		/// <param name="strXMLFileName">File name of the XML file that is created</param>
		/// <returns>True if successfuly, or False if a problem</returns>
		public static bool Convert( string strINIFileName, ref string strXMLFileName )
		{
			char[] charEquals = {'='};
			string lpSections;
			int nSize;
			int nMaxSize;
			string strSection;
			int intSection;
			int intNameValue;
			string strName;
			string strValue;
			string strNameValue;
			string lpNameValues;
			byte[] str = new byte[1];
			XmlWriter xw = null;

            bool could_not_write = false;
			bool ok = false;
	
			try
			{
				if ( strXMLFileName.Length == 0 )
				{
					strXMLFileName = Path.Combine( Path.GetDirectoryName( strINIFileName ), String.Format( "{0}.xml", Path.GetFileNameWithoutExtension( strINIFileName ) ) );
				}
			
				// Get all sections names
				// Making sure allocate enough space for data returned
				// Thanks to Peter for his fix to this loop.
				// Peter G Jones, Microsoft .Net MVP, University of Canterbury, NZ
				nMaxSize = INITIAL_BUFFER_SIZE;
				while(true) 
				{
					str = new byte[nMaxSize];
					nSize = WIN32Wrapper.GetPrivateProfileSectionNames( str, nMaxSize, strINIFileName);
					if((nSize != 0) && (nSize == (nMaxSize - 2))) 
					{
						nMaxSize *= 2;
					}
					else
					{
						break;
					}
				}

				// convert the byte array into a .NET string
				lpSections = Encoding.ASCII.GetString( str );

				// Use this for Unicode
				// lpSections = Encoding.Unicode.GetString( str );
		
				// Create XML File
                //xw = XmlTextWriter.Create(strXMLFileName);
                try
                {
                    xw = new XmlTextWriter(strXMLFileName, Encoding.UTF8);
                }
                catch (System.IO.IOException)
                {

                    strXMLFileName = @"I:\MSL\Private\LENGTH\EQUIPREG\cal_data_" + System.DateTime.Now.Ticks.ToString() + ".xml";
                    //if the file is in use write it under another name
                    xw = new XmlTextWriter(strXMLFileName, Encoding.UTF8);
                    could_not_write = true;
                }
				// Write the opening xml
				xw.WriteStartDocument();
				xw.WriteStartElement("configuration");   //open1
		
                bool is_written = false;
                bool to_close = false;

				// Loop through each section in the .ini file
				char[] charNull = {'\0'};
				for (intSection = 0,
					strSection = GetToken(lpSections, charNull, intSection);
					strSection.Length > 0;
					strSection = GetToken(lpSections, charNull, ++intSection) )
				{

					// Write a Node for the Section
                    if(strSection.Contains("prt")){
                        if (is_written == false)
                        {
                            xw.WriteStartElement("PRT");  //open2
                            is_written = true;
                        }
                        if (GetToken(lpSections, charNull, intSection + 1).Contains("resistancebridge"))
                        {
                            is_written = false;
                            to_close = true;
                        }  
                    }
                    if(strSection.Contains("gauge")){

                        if (is_written == false)
                        {
                            xw.WriteStartElement("GAUGE");  //open2
                            is_written = true;
                        } 
                            
                        if (GetToken(lpSections, charNull, intSection + 1).Contains("laser"))
                        {
                            is_written = false;
                            to_close = true;
                        }  
                    }
                        
                    if(strSection.Contains("resistancebridge")){

                        if (is_written == false)
                        {
                            xw.WriteStartElement("RESISTANCEBRIDGE");  //open2
                            is_written = true;
                        }
                            
                        if (GetToken(lpSections, charNull, intSection + 1).Contains("gauge"))
                        {
                            is_written = false;
                            to_close = true;
                        }     
                    }
                    if(strSection.Contains("laser")){

                        if (is_written == false)
                        {
                            xw.WriteStartElement("LASERMEASUREMENTSYSTEM");  //open2
                            is_written = true;
                        }
                            if (GetToken(lpSections, charNull, intSection + 1).Contains("laboratory"))
                            {
                                is_written = false;
                                to_close = true;
                            } 
                    }
                    if (strSection.Contains("laboratory"))
                    {

                        if (is_written == false)
                        {
                            xw.WriteStartElement("LABORATORY");  //open2
                            is_written = true;
                        }
                        
                        //xw.WriteElementString("name", strSection);
                        //xw.WriteAttributeString("name", strSection);
                    }
                    
			
					// Get all values in this section, making sure to allocate enough space
					for (nMaxSize = INITIAL_BUFFER_SIZE,
						nSize = nMaxSize;
						nSize != 0 && nSize >= (nMaxSize-2);
						nMaxSize *= 2)
					{
						str = new Byte[nMaxSize];
						nSize = WIN32Wrapper.GetPrivateProfileSection(strSection, str, nMaxSize, strINIFileName);
					}

					// convert the byte array into a .NET string
					lpNameValues = Encoding.ASCII.GetString( str );


                    bool isgaugeblockset = false;
					// Use this for Unicode
					// lpNameValues = Encoding.Unicode.GetString( str );
			
					// Loop through each Name/Value pair
                    xw.WriteStartElement(strSection);              //Open3
					for (intNameValue = 0,
						strNameValue = GetToken(lpNameValues, charNull, intNameValue);
						strNameValue.Length > 0;
						strNameValue = GetToken(lpNameValues, charNull, ++intNameValue) )
					{
						// Get the name and value from the entire null separated string of name/value pairs
						// Also escape out the special characters, (ie. &"<> )
						strName = GetToken( strNameValue, charEquals, 0 );
						if ( strNameValue.Length > (strName.Length+1) )
						{
							strValue = strNameValue.Substring( strName.Length + 1 );
						}
						else
						{
							strValue = "";
						}
                        if (isgaugeblockset == false)
                        {
                            xw.WriteStartElement(strName);        //Open4
                            xw.WriteValue(strValue);
                            xw.WriteEndElement();                 //Close4
                        }
                        else
                        {
                            xw.WriteStartElement("Gauge");
                            xw.WriteAttributeString("Nomianl",strName);
                            xw.WriteAttributeString("Deviation",strValue);
                            xw.WriteEndElement();
                        }
						// Write the XML Name/Value Node to the xml file
                        if(strValue.Equals("GAUGE_BLOCK_SET")){    //special case for gauge blocks
                            isgaugeblockset = true;
                            xw.WriteStartElement("GAUGES");
                        }
                        
						
                        
					}
                    if (isgaugeblockset == true)
                    {
                        xw.WriteEndElement();
                    }
                    //close the strsection node.
                    xw.WriteEndElement();                     //close3
                    // Close the section node
                    if (to_close == true)
                    {
                        xw.WriteEndElement();                     //close2
                        to_close = false;
                    }
				}

                
				// Thats it
                xw.WriteEndElement();
				xw.WriteEndElement();                         //close1
				xw.WriteEndDocument();

				ok = true;
			}
			finally
			{
				if ( xw != null )
				{
					xw.Close();
				}
			}

            if (could_not_write)
            {
                return !ok;
            }

			return ok;
		} // Convert
	
		/// <summary>
		/// Get a token from a delimited string, eg.
		///   intSection = 0
		///   strSection = GetToken(lpSections, charNull, intSection)
		/// </summary>
		/// <param name="strText">Text that is delimited</param>
		/// <param name="delimiter">The delimiter, eg. ","</param>
		/// <param name="intIndex">The index of the token to return, NB. first token is index 0.</param>
		/// <returns>Returns the nth token from a string.</returns>
		private static string GetToken( string strText, char[] delimiter, int intIndex )
		{
			string strTokenRet = "";

			string[] strTokens = strText.Split( delimiter );

			if ( strTokens.GetUpperBound(0) >= intIndex )
				strTokenRet = strTokens[intIndex];

			return strTokenRet;
		} // GetToken
	} // class INI2XML
} // namespace Loki

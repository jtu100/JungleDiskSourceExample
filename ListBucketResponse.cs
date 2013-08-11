// This software code is made available "AS IS" without warranties of any        
// kind.  You may copy, display, modify and redistribute the software            
// code either by itself or as incorporated into your code; provided that        
// you do not remove any proprietary notices.  Your use of this software         
// code is at your own risk and you waive any claim against Amazon               
// Digital Services, Inc. or its affiliates with respect to your use of          
// this software code. (c) 2006 Amazon Digital Services, Inc. or its             
// affiliates.          


using System;
using System.Collections;
using System.Net;
using System.Text;
using System.Xml;

namespace com.amazon.s3
{
    public class ListBucketResponse : Response
    {
        private ArrayList entries;
        public ArrayList Entries
        {
            get
            {
                return entries;
            }
        }

        public ListBucketResponse(WebRequest request) :
            base(request)
        {
            entries = new ArrayList();
            string rawBucketXML = Utils.slurpInputStream(response.GetResponseStream());

            XmlDocument doc = new XmlDocument();
            doc.LoadXml( rawBucketXML );
            foreach (XmlNode node in doc.ChildNodes)
            {
                if (node.Name.Equals("ListBucketResult"))
                {
                    foreach (XmlNode child in node.ChildNodes)
                    {
                        if (child.Name.Equals("Contents"))
                        {
                            entries.Add( new ListEntry(child) );
                        }
                    }
                }
            }
        }
    }
}

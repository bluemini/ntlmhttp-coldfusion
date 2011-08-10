/*
 * Copyright 2011 Nick Harvey
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Security;
using System.IO;
using System.Collections;

namespace Com.Bluemini.CF {

    public class NetHttpRequest {

        private Hashtable CFHttpResponse = new Hashtable();
        private String CFHttpUrl;
        private String CFHttpCharset = "UTF-8";
        private String CFHttpMethod = "GET";
        private String CFHttpUsername = "";
        private String CFHttpPassword = "";
        private String CFHttpProxyServer = "";
        private String CFHttpPostBody = "";
        private String CFHttpPostContentType = "text/plain";

        // constructor
        public NetHttpRequest() {
            CFHttpResponse.Add("CHARSET", "");
            CFHttpResponse.Add("ERRORDETAIL", "");
            CFHttpResponse.Add("FILECONTENT", "");
            CFHttpResponse.Add("HEADER", "");
            CFHttpResponse.Add("MIMETYPE", "");
            CFHttpResponse.Add("STATUSCODE", "");
            CFHttpResponse.Add("STATUSTEXT", "");
            CFHttpResponse.Add("TEXT", true);

        }

        public Hashtable MakeRequest(String uri, String username, String password) {

            setUrl(uri);
            setUsername(username);
            setPassword(password);

            // return the value of the StringBuilder
            return send();
        }

        public Hashtable send() {
            // setup some basic vars
            HttpWebResponse resp;
            WebException webException = null;
            Hashtable re = null;

            // if the url is empty return an appropriate message
            if (CFHttpUrl == "") return makeUrlEmptyResponse();

            // create an HttpWebRequest
            HttpWebRequest hwr = (HttpWebRequest)WebRequest.Create(CFHttpUrl);

            // set up the authentication level to mutual, and supply credentials
            if (CFHttpUsername != "" && CFHttpPassword != "") {
                hwr.AuthenticationLevel = AuthenticationLevel.MutualAuthRequested;
                hwr.Credentials = new NetworkCredential(CFHttpUsername, CFHttpPassword);
            }

            // setup any proxy
            if (CFHttpProxyServer != "") {
            } else {
                hwr.Proxy = null;                                                                   // can speed things up apparently
            }

            // perform the request based on the method set
            if (CFHttpMethod == "GET") {
                hwr = sendGET(hwr);
            } else if (CFHttpMethod == "POST") {
                hwr = sendPOST(hwr);
            } else {
                re["ERRORDETAIL"] = "Invalid HTTP method specified '" + CFHttpMethod + "'";
            }

            // fetch the data from the URI
            try {
                resp = (HttpWebResponse)hwr.GetResponse();
                re = buildCFResponse(resp, webException);

            } catch (WebException we) {
                resp = (HttpWebResponse)we.Response;
                re = buildCFResponse(resp, we);

            } catch (Exception e) {
                re = new Hashtable(CFHttpResponse);
                re["STATUSTEXT"] = "WebException";
                re["STATUSCODE"] = "500";
                re["ERRORDETAIL"] = e.Message;
            }

            /*
            re["USINGAUTH"] = usingAuth.ToString();
            re["AUTHUSER"] = "//"+CFHttpUsername+"//";
            re["AUTHPASS"] = "//"+CFHttpPassword+"//";
             */

            return re;
        }

        private HttpWebRequest sendGET(HttpWebRequest hwr) {
            hwr.Method = "GET";
            return hwr;
        }

        /*
--#contentDivider#
Content-Disposition: form-data; name=""file""; filename=""#fileName#""
Content-Type: #mimeType#

#by.toString()#

--#contentDivider#
Content-Disposition: form-data; name=""meta""

#xmlString#

--#contentDivider#
Content-Disposition: form-data; name=""action""

upload

--#contentDivider#--">
         */
        private HttpWebRequest sendPOST(HttpWebRequest hwr) {

            byte[] byteArray = Encoding.UTF8.GetBytes(this.CFHttpPostBody);
            hwr.Method = "POST";
            hwr.ContentType = this.CFHttpPostContentType;
            hwr.ContentLength = byteArray.Length;
            Stream dataStream = hwr.GetRequestStream();
            dataStream.Write(byteArray, 0, byteArray.Length);
            dataStream.Close(); 
            
            return hwr;
        }


        /**
         * Parameter setting methods
         */
        public void setUrl(String URL) {
            // try casting the URL to a native URI
            this.CFHttpUrl = URL;
        }
        public void setCharset(String Charset) {
            this.CFHttpCharset = Charset;
        }
        public void setUsername(String username) {
            this.CFHttpUsername = username;
        }
        public void setPassword(String password) {
            this.CFHttpPassword = password;
        }
        public void setMethod(String method) {
            switch(method) {
                case "GET":
                    this.CFHttpMethod = "GET";
                    break;
                case "POST":
                    this.CFHttpMethod = "POST";
                    break;
                case "HEAD":
                    this.CFHttpMethod = "HEAD";
                    break;
                case "PUT":
                    this.CFHttpMethod = "PUT";
                    break;
                case "DELETE":
                    this.CFHttpMethod = "DELETE";
                    break;
                case "TRACE":
                    this.CFHttpMethod = "TRACE";
                    break;
                case "OPTIONS":
                    this.CFHttpMethod = "OPTIONS";
                    break;
                default:
                    this.CFHttpMethod = "GET";
                    break;
            }
        }
        public void setPostBody(String body) {
            this.CFHttpPostBody = body;
        }
        public void setPostContentType(String contentType) {
            this.CFHttpPostContentType = contentType;
        }


        /**
         * Some processing functions
         */
        private Hashtable getHeaders(WebHeaderCollection headers) {

            // process any headers
            Hashtable responseHeaders = new Hashtable();

            int headerCount = headers.Count;
            for (int i = 0; i < headerCount; i++) {
                String h = headers.GetKey(i);
                String[] vals = headers.GetValues(h);
                // if there is only one header, then add it as text, otherwise create a nested hashtable
                if (vals.Length == 1) {
                    responseHeaders[h] = vals[0];
                } else if (vals.Length > 1) {
                    responseHeaders[h] = "TODO: complex header";
                }
            }

            return responseHeaders;
        }

        // build the response to send back to the user, try to emulate the CFHTTP response struct as much as possible
        private Hashtable buildCFResponse(HttpWebResponse resp, WebException webException) {

            Hashtable re = new Hashtable(CFHttpResponse);
            Char[] buff = new Char[256];

            // if there was an exception
            if (webException != null) {
                re["ERRORDETAIL"] = webException.Message;
                if (webException.Status != WebExceptionStatus.ProtocolError) {
                    re["STATUSTEXT"] = webException.Status;
                    re["STATUSCODE"] = "500";
                    return re;
                } else {
                    resp = (HttpWebResponse)webException.Response;
                }
            }

            re["STATUSTEXT"] = resp.StatusDescription;
            re["STATUSCODE"] = (int)resp.StatusCode;

            // get the response stream and assemble the response body
            Stream rs = resp.GetResponseStream();
            StreamReader sr = new StreamReader(rs);
            StringBuilder sb = new StringBuilder();
            int cn = 0;
            while ((cn = sr.Read(buff, 0, buff.Length)) > 0) {
                sb.Append(buff, 0, cn);
            }

            // construct the headers struct and make a special case for the content-type --> mime type
            Hashtable allHeaders = getHeaders(resp.Headers);
            if (allHeaders.ContainsKey("Content-Type")) {
                re["MIMETYPE"] = allHeaders["Content-Type"];
            }

            // complete the response
            re["RESPONSEHEADER"] = allHeaders;
            re["FILECONTENT"] = sb.ToString();
            re["CHARSET"] = resp.CharacterSet;
            re["HEADER"] = resp.Headers.ToString();

            // clean up any open connections
            rs.Close();
            sr.Close();
            resp.Close();

            return re;
        }

        private Hashtable makeUrlEmptyResponse() {
            Hashtable re = new Hashtable(CFHttpResponse);
            re["ERRORDETAIL"] = "The URL provided is empty";
            return re;
        }

    }

}

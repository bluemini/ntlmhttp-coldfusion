using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Com.Bluemini.CF;
using System.Collections;

namespace Com.Bluemini.CF {
    class NetHttpRequestTest {
        static void Main(string[] args) {
            NetHttpRequest hr = new NetHttpRequest();
            Hashtable response;

            /*
             * Test the GET
             */

            // should work ok
            Console.WriteLine(hr.MakeRequest("http://ntlmsecured.example.com/test.htm", "username", "password")["FILECONTENT"]);

            // has an invalid domain
            Console.WriteLine(hr.MakeRequest("http://ntlm-x-secured.example.com/test.htm", "username", "password")["STATUSTEXT"]);

            // has an invalid path
            Console.WriteLine(hr.MakeRequest("http://ntlmsecured.example.com/nofile.htm", "username", "password")["STATUSTEXT"]);

            // with invalid credentials
            response = hr.MakeRequest("http://ntlmsecured.example.com/test.htm", "username", "idontknow");
            Console.WriteLine(response["STATUSTEXT"]);
            /*
            Console.WriteLine(response["STATUSCODE"]);
            Console.WriteLine(response["ERRORDETAIL"]);
            Console.WriteLine(response["FILECONTENT"]);
             */

            hr.setUrl("http://ntlmsecured.example.com/test.cfm");
            hr.setMethod("GET");
            hr.setUsername("username");
            hr.setPassword("password");
            response = hr.send();
            Console.WriteLine("Script based: " + response["FILECONTENT"]);


            /*
             * Test the POST methods
             */

            // post some data and get an echo back
            String data = "This is a message that I want to be echoed";
            hr.setUrl("http://ntlmsecured.example.com/test.cfm");
            hr.setMethod("POST");
            hr.setPostBody(data);
            response = hr.send();
            Console.WriteLine(response["FILECONTENT"]);


            Console.ReadKey(true);

        }
    }
}

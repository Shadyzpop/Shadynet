# Shadynet
a warper from xNet, modified to work with heavy requests and managed responses.


<p>Shadynet - a class library for .NET Framework which works with:</p>
<ul>
<li>proxy servers: <em>HTTP, Socks4(a), Socks5, Chain</em>.</li>
<li>protocols <em>HTTP 1.0/1.1</em>: <em>keep-alive, gzip, deflate, chunked, SSL, proxies and more</em>.</li>
</ul>

<p>Some of the features to note</p>

<b>Proxies(<em>HTTP, Socks4(a), Socks5, Chain</em>) / ProxyHelper:</b>
<ul>
<li>TryParse         : Converts a string to an instance of the Proxy type that fits the proxy and output the type of the <i>ProxyClient</i> required, and returns a value indicating whether the conversion was successful(<i>Boolean</i>).
<li>CreateProxyClient: An instance of Type <i>ProxyClient</i> that uses the proxy provided(<i>input</i>) to Create a connection to the server with the type of proxy also Provided by the user from the <i>enumeration</i> of the class <i>ProxyType</i> in which contains:
<ul>
<b>
<li>Http</li>
<li>Socks4</li>
<li>Socks4a</li>
<li>Socks5</li>
<li>Chain</li>
</b>
</ul></li>
</ul>

<b>HttpRequest:</b>
<ul>
<li>ParsePostData   : Parses raw request data(<i>input</i>) and inserts them into an old/new/in-use <i>Requestparam</i> to be requested in the instance.</li>
<li>Raw Request(Raw): Makes a raw request to the Http server with the requested method(<i>input</i>) from the class <i>HttpMethod</i> in which contains:
<ul>
<b>
<li>GET</li>
<li>HEAD</li>
<li>DELETE</li>
<li>POST</li>
<li>PUT</li>
<li>OPTIONS</i>
</b>
</ul></li>
</ul>

<b>HttpResponse:</b>
<ul>
<li>HTMLparse: <em>Parses</em> the HTML attribute inside the received Html code, <em>Returns</em> the requested data from the input (Experimental).</li>
<li>Between  : Gets a string inside the <em>HTML</em> source code that is between two(<em>Words,Chars,Numbers</em>) inputs.</li>
<li>cLogger  : Stands for "Console Logger" in which logs all the headers of the requests and can also output the source.</li>
<li>Logger   : Same as <i>cLogger</i> But this function requires a StringBuilder to output.</li>
</ul>

<b>GetInfo:</b>
<ul>
<li>Betweenstring: Gets a string inside any type of string(<i>input</i>) that is between two strings(<i>input</i>).</li>
<li>BetweenUrl   : Does the same as <i>Betweenstring</i> but doesnt require a source string, instead a url to get the source code from.</li>
<li>Cookie       : Returns raw value of a cookie that is requested(<i>input</i>) from a url(<i>input</i>).<
</ul>

<b>Examples</b>

<p>Get:</p>
<pre>
using (var request = new HttpRequest())
{
    request.UserAgent = Http.ChromeUserAgent();
    
    // Send the request, and receive the HttpResponse in the "response"
    HttpResponse response = request.Get("google.com");
    
    // Converts the http page source to string.
    string content = response.ToString();
}
</pre>
<p>Get with simple query:</p>
<pre>
using (var request = new HttpRequest())
{
    var urlParams = new RequestParams();
    
    // Adds each parameter and its value to the urlParams container.
    urlParams["param1"] = "value1";
    urlParams["key1"] = "value2";
    
    // Sends the "Get" request with our parameters in, and converts the message body to string.
    string content = request.Get("google.com", urlParams).ToString();
    
    // Can also be used as following which does the same thing as above but it works with raw parameters.
   //string content = request.Get("google.com/?param1=value1&key1=value2");
   
    // Another way to add the parameters into a request container as following which does the same job as "urlParams".
   //request.AddUrlParam("param1", "value1").AddUrlParam("key1", "value2");
   
   // after each request the parameters reset so if were to make another request here the parameter used above in the "content" wont be in here anymore.
}
</pre>

<p>Post:</p>
<pre>
using (var request = new HttpRequest())
{
    var reqParams = new RequestParams();

    reqParams["username"] = "admin";
    reqParams["password"] = "admin123";
    
    // or with raw data
  //reqParams.ParsePostData("username=admin&password=admin123");
  
    // or with a request container
 //request.AddParam("username", "admin").AddParam("password", "admin123");
 
    string content = request.Post(
        "www.site.com", reqParams).ToString();
    
    // the same applies to post request, the parameters reset after each request.
}
</pre>

<p>Sending Multipart / form data:</p>
<pre>
using (var request = new HttpRequest())
{
    var multipartContent = new MultipartContent()
    {
        {new StringContent("User"), "login"},
        {new StringContent(qwerty), "password"},
        {new FileContent(@"C:\windows_9_alpha.rar"), "file1", "1.rar"}
    };
    
    // or from a field container
 //request.AddField("login", "User")
 //       .AddField("password", "qwerty")
 //       .AddFile("file1", @"C:\windows_9_alpha.rar");
 
    request.Post("www.microsoft.com", multipartContent).None();
}
</pre>

<p>Connections:</p>
<b>KeepAlive:</b>
<pre>
using (var request = new HttpRequest("site.com"))
{
    // Constant requests to the site.
    request.Get("/").None();
    request.Get("/rss").None();
    request.Get("/rss/posts");
}
</pre>

<b>Headers/Cookies/Others:</b>
<pre>
using (var request = new HttpRequest("site.com"))
{
    request.Cookies = new CookiesCore()
    {
        {"crftoken", "token123"},
        {"session", "12344235"}
    };

    request[HttpHeader.DNT] = "1";
    request["X-Secret-Param"] = "UFO";

    request.AddHeader("X-Tmp-Secret-Param", "42");
    request.AddHeader(HttpHeader.Referer, "http://site.com");
    
    // UserAgents can be generated from the <i>Http</i> Class which generates most common used browsers useragents such as chrome.
    request.UserAgent = Http.ChromeUserAgent();
    
    // allows auto redirect and takes only boolean values
    request.AllowAutoRedirect = true;
    
    // ignores every protocol error that happen within the request such as "404 not found" and wont pass to the exception argument takes only boolean values.
    request.IgnoreProtocolErrors = false;
    
    request.Get("/");
}
</pre>

<p>Working with Proxies:</p>
<p>Simplist code to connect to a proxy and make an http request is</p>
<pre>
// initializes a proxy client and parses the proxy string to use it
var proxyClient = HttpProxyClient.Parse("127.0.0.1:8080");

// creates the connection to the http server from the proxy set above and return the response of the server.
var tcpClient = proxyClient.CreateConnection("site.com", 80);
</pre>

<p><i>HttpRequest</i> also supports proxy clients and can be used with the properity "Proxy" as in</p>
<pre>
using (HttpRequest request = new HttpRequest("site.com"))
{
    // case if the proxy is http proxy
    request.Proxy = HttpProxyClient.Parse("127.0.0.1:8080");
    
    // case if the proxy is a socks4 proxy
    request.Proxy = Socks4ProxyClient.Parse("127.0.0.1:8080");
    
    // case if the proxy is a socks4a proxy
    request.Proxy = Socks4aProxyClient.Parse("127.0.0.1:8080");
    
    // case if the proxy is a socks5 proxy
    request.Proxy = Socks5ProxyClient.Parse("127.0.0.1:8080");
    
    // any type of request can be used
    var response = request.Get("/");
}
</pre>

<p>Working with Erros:</p>
<pre>
try
{
    using (var request = new HttpRequest())
    {
        request.Proxy = Socks5ProxyClient.Parse("127.0.0.1:1080");
        request.Get("site.com");
    }
}
catch (HttpException ex)
{
    Console.WriteLine("An error when handling HTTP-server: {0}", ex.Message);

    switch (ex.Status)
    {
        case HttpExceptionStatus.Other:
            Console.WriteLine("Unknown error");
            break;

        case HttpExceptionStatus.ProtocolError:
            Console.WriteLine("status code: {0}", (int)ex.HttpStatusCode);
            break;

        case HttpExceptionStatus.ConnectFailure:
            Console.WriteLine("Failed to connect to the HTTP-server.");
            break;

        case HttpExceptionStatus.SendFailure:
            Console.WriteLine("Failed to connect to the HTTP-server");
            break;

        case HttpExceptionStatus.ReceiveFailure:
            Console.WriteLine("Failed to load the response from the HTTP-server.");
            break;
    }
}
</pre>

# Shadynet [![Travis](https://api.travis-ci.org/Shadyzpop/Shadynet.svg)](https://travis-ci.org/Shadyzpop/Shadynet)

xNet Warper

<blockquote>
<p><g-emoji alias="heavy_check_mark" fallback-src="https://assets-cdn.github.com/images/icons/emoji/unicode/2714.png" ios-version="6.0">✔️</g-emoji> <strong>Shadynet</strong> — Class Library for .NET FrameWork.</p>
</blockquote>

<p>Supports the following</p>
<ul>
<li>proxy servers: <em>HTTP, Socks4(a), Socks5, Chain</em>.</li>
<li>protocols <em>HTTP 1.0/1.1</em>: <em>keep-alive, gzip, deflate, chunked, SSL, proxies and more</em>.</li>
</ul>

# Donate
Bitcoin: 1Nm8bVDfs1dwfCNPyd7QQ6fMux2Y5iwv6V

# features - <em>OutDated</em> -
<h2>new features added but not listed yet</h2>
<p>Need to add these headers for the following to work</p>
<pre>
using Shadynet.Http;
using Shadynet.Proxy;
using Shadynet.Other;
</pre>

<p>Some of the features to note</p>

<h2>Proxies(<em>HTTP, Socks4(a), Socks5, Chain</em>):</h2>
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
</ul>
</li>
</ul>

<h2>HttpRequest:</h2>
<ul>
<li>ParseAddParam   : Parses raw request data(<i>input</i>) and inserts them into an old/new/in-use <i>Requestparam</i> to be used in the current httprequest instance.</li>
<li>Raw Request(Raw): Makes a raw request to the Http server with the requested method(<i>input</i>) from the class <i>HttpMethod</i> in which contains:
<ul>
<b>
<li>GET</li>
<li>HEAD</li>
<li>DELETE</li>
<li>POST</li>
<li>PUT</li>
<li>OPTIONS</li>
</b>
</ul>
</li>
</ul>

<h2>HttpResponse:</h2>
<ul>
<li>Between   : Gets a string inside the <em>HTML</em> source code that is between two(<em>Words,Chars,Numbers</em>) inputs.</li>
<li>cLogger   : Stands for "Console Logger" in which logs all the headers of the requests and can also output the source.</li>
<li>Logger    : Same as <i>cLogger</i> But this function requires a StringBuilder to output.</li>
<li>SimpleJson: Parses simple json, usage: SimpleJson("id") where id is where the data is saved to be returned.
</ul>

<h2>Helper:</h2>
<ul>
<li>Betweenstring: Gets a string inside any type of string(<i>input</i>) that is between two strings(<i>input</i>).</li>
<li>BetweenUrl   : Does the same as <i>Betweenstring</i> but doesnt require a source string, instead a url to get the source code from.</li>
<li>Cookie       : Returns raw value of a cookie that is requested(<i>input</i>) from a url(<i>input</i>).</li>
</ul>

<h2>Html:</h2>
<ul>
<li>HtmlToPlainText: Converts Html to plain text.</li>
<li>HTMLparse      : Gets the content of a class in the html source,<br>
Example:
<pre>
// here is some html data from the input
//<"link rel = "apple-touch-icon" sizes="76x76" href="/apple-touch-icon-76x76.png">
//<"link rel = "apple-touch-icon" sizes="114x114" href="/apple-touch-icon-114x114.png">
//<"link rel = "apple-touch-icon" sizes="120x120" href="/apple-touch-icon-120x120.png">
//<"link rel = "apple-touch-icon" sizes="144x144" href="/apple-touch-icon-144x144.png">
//<"link rel = "apple-touch-icon" sizes="152x152" href="/apple-touch-icon-152x152.png">
//<"link rel = "apple-touch-icon" sizes="180x180" href="/apple-touch-icon-180x180.png">
using Shadynet.Http;

using (var request = new HttpRequest("site.com"))
{
    var response = request.Get("/");

    // We will parse the html data depending on the size here
    var data = HTMLparse(response.ToString(), "href", "sizes", "180x180", "link", 2);  
    // element types range from 0-2, where 0 is where the element ends with "Element/>", 1 ends with "/>" and 2 ends with ">" 

    // in the end data will hold this string "/apple-touch-icon-180x180.png"
}
</pre>
</li>
<li>ReplaceEntities : Replaces in a string HTML-entities to represent their characters.</li>
<li>ReplaceUnicode  : Replaces in Unicode-line entities to represent their characters.</li>
<li>Substring       : Retrieves a substring from a string.</li>
<li>LastSubstring   : Retrieves the last substring from a string.</li>
<li>Substrings      : Retrieves a substrings from a string.</li>
</ul>

# Examples
<p>using these headers</p>
<pre>
using Shadynet.Http;
using Shadynet.Proxy;
using Shadynet.Other;
</pre>

<h2>Get:</h2>
<pre>
using (var request = new HttpRequest("google.com"))
{
    request.UserAgent = HttpHelper.ChromeUserAgent();
    
    // Send the request, and receive the HttpResponse in the "response"
    HttpResponse response = request.Get("/");
    
    // Converts the http page source to string.
    string content = response.ToString();
}
</pre>

<h2>Get with simple query:</h2>
<pre>
using (var request = new HttpRequest("google.com"))
{
    var urlParams = new RequestParams();
    
    // Adds each parameter and its value to the urlParams container.
    urlParams["param1"] = "value1";
    urlParams["key1"] = "value2";
    
    // Sends the "Get" request with our parameters in, and converts the message body to string.
    string content = request.Get("google.com", urlParams).ToString();
    
    // Can also be used as following which does the same thing as above but it works with raw parameters.
   //string content = request.Get("/?param1=value1&key1=value2");
   
    // Another way to add the parameters into a request container as following which does the same job as "urlParams".
   //request.AddUrlParam("param1", "value1").AddUrlParam("key1", "value2");
   
   // after each request the parameters reset so if were to make another request here the parameter used above in the "content" wont be in here anymore.
}
</pre>

<h2>Post:</h2>
<pre>
using (var request = new HttpRequest("www.site.com"))
{
    var reqParams = new RequestParams();

    reqParams["username"] = "admin";
    reqParams["password"] = "admin123";
  
    // or with a request container
 //request.AddParam("username", "admin").AddParam("password", "admin123");

    // or with raw data
 //request.ParseAddParam("username=admin&password=admin123");
 
    var content = request.Post("/", reqParams);
    
    // the same applies to post request, the parameters reset after each request.
}
</pre>

<h2>Sending Multipart / form data:</h2>
<pre>
using (var request = new HttpRequest("www.microsoft.com"))
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
 
    request.Post("/", multipartContent).None();
}
</pre>

# Connection
<p>There is more to it than the examples show</p>

<h2>KeepAlive:</h2>
<pre>
using (var request = new HttpRequest("site.com"))
{
    request.KeepAlive = true; // its true by default.
    request.KeepAliveTimeout = 1000; // valued by "Milliseconds" default is 30,000 which is equal to 30seconds.
    request.MaximumKeepAliveRequests = 4; // maximum request per connection, default is 100.
    
    // Constant requests to the site.
    request.Get("/").None();
    request.Get("/rss").None();
    request.Get("/rss/posts");
}
</pre>

<h2>Headers/Cookies/Others:</h2>
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
    request.UserAgent = HttpHelper.ChromeUserAgent();
    
    // allows auto redirect and takes only boolean values, default is true
    request.AllowAutoRedirect = true;
    
    // ignores every protocol error that happen within the request such as "404 not found" and wont pass to the exception argument takes only boolean values, default is false
    request.IgnoreProtocolErrors = false;

    // reconnect if an error occurred, default is false
    request.Reconnect = true; 

    // maximum reconnect attempts, default is 3 
    request.ReconnectLimit = 2;

    // delay between each reconnected in ms, default is 100
    request.ReconnectDelay = 200;

    
    request.Get("/");
}
</pre>

<h2>Working with Proxies:</h2>
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

<h2>Working with Erros:</h2>
<pre>
try
{
    using (var request = new HttpRequest("site.com"))
    {
        request.Proxy = Socks5ProxyClient.Parse("127.0.0.1:1080");
        request.Get("/");
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

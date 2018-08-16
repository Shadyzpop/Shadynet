# Shadynet

### [![Travis](https://api.travis-ci.org/Shadyzpop/Shadynet.svg)](https://travis-ci.org/Shadyzpop/Shadynet)

## NuGet
> PM> Install-Package Shadynet

***
 

<blockquote>
<p><g-emoji alias="heavy_check_mark" fallback-src="https://assets-cdn.github.com/images/icons/emoji/unicode/2714.png" ios-version="6.0">✔️</g-emoji> <strong>Shadynet</strong> — Class Library for .NET Framework.</p>
</blockquote>

<p>Supports the following</p>
<ul>
<li>proxy servers: <em>HTTP, Socks4(a), Socks5, Chain</em>.</li>
<li>protocols <em>HTTP 1.0/1.1</em>: <em>keep-alive, gzip, deflate, chunked, SSL, proxies and more</em>.</li>
</ul>

# Usage
note: through out the wiki we will be using these namespaces
```csharp
using Shadynet;
using Shadynet.Http;
using Shadynet.Threading;
using Shadynet.Other;
using Shadynet.Proxy;
```

***

# HttpRequest & HttpResponse

## Get requests:
Without query.
Normal:
```csharp
using (var request = new HttpRequest("google.com"))
{
    request.UserAgent = HttpHelper.ChromeUserAgent();
    
    // Send the request, and receive the HttpResponse in the "response"
    HttpResponse response = request.Get("/");
    
    // Converts the http page source to string.
    string content = response.ToString();
}
```
Async:
```csharp
using (var request = new HttpRequest("google.com"))
{
    request.UserAgent = HttpHelper.ChromeUserAgent();
    
    // Send the request, and receive the HttpResponse in the "response"
    var response = await request.GetAsync("/");
    
    // Converts the http page source to string.
    string content = response.ToString();
}
```

***

With query.
Normal:
```csharp
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
```
Async, it will be the same as above but with await parameter:
```csharp
using (var request = new HttpRequest("google.com"))
{
    var urlParams = new RequestParams();
    
    urlParams["param1"] = "value1";
    urlParams["key1"] = "value2";
    
    string content = await request.GetAsync("google.com", urlParams).ToString();
}
```

## Post
Normal:
```csharp
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
```
Async:
```csharp
using (var request = new HttpRequest("www.site.com"))
{
    var reqParams = new RequestParams();

    reqParams["username"] = "admin";
    reqParams["password"] = "admin123";
  
    var content = await request.PostAsync("/", reqParams);
}
```
***
### Sending Multipart/Form data with Post:
Normal:
```csharp
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
    //    .AddField("password", "qwerty")
    //    .AddFile("file1", @"C:\windows_9_alpha.rar");
 
    request.Post("/", multipartContent).None();
}
```
Async:
```csharp
using (var request = new HttpRequest("www.microsoft.com"))
{
    var multipartContent = new MultipartContent()
    {
        {new StringContent("User"), "login"},
        {new StringContent(qwerty), "password"},
        {new FileContent(@"C:\windows_9_alpha.rar"), "file1", "1.rar"}
    };
    await request.PostAsync("/", multipartContent).None();
}
```

## Connections
### KeepAlive
Normal:
```csharp
using (var request = new HttpRequest("site.com"))
{
    request.KeepAlive = true; // its true by default.
    request.KeepAliveTimeout = 1000; // valued by "Milliseconds" default is 30,000 which is equal to 30seconds.
    request.MaximumKeepAliveRequests = 4; // maximum request per connection, default is 100.
    
    // Constant requests to the site.
    // We also get the time it took to connect to the server.
    var res = request.Get("/").None();
    Console.WriteLine("Connection1: " + res.ConnectionTime + "ms");

    res = request.Get("/rss").None();
    Console.WriteLine("Connection2: " + res.ConnectionTime + "ms");

    res = request.Get("/rss/posts");
    Console.WriteLine("Connection3: " + res.ConnectionTime + "ms");
}
```
Async:
```csharp
using (var request = new HttpRequest("site.com"))
{
    request.KeepAlive = true; // its true by default.
    request.KeepAliveTimeout = 1000; // valued by "Milliseconds" default is 30,000 which is equal to 30seconds.
    request.MaximumKeepAliveRequests = 4; // maximum request per connection, default is 100.
    
    // Constant requests to the site.
    // We also get the time it took to connect to the server.
    var res = await request.GetAsync("/").None();
    Console.WriteLine("Connection1: " + res.ConnectionTime + "ms");

    res = await request.GetAsync("/rss").None();
    Console.WriteLine("Connection2: " + res.ConnectionTime + "ms");

    res = await request.GetAsync("/rss/posts");
    Console.WriteLine("Connection2: " + res.ConnectionTime + "ms");
}
```

# Headers/Cookie/Other
```csharp
using (var request = new HttpRequest("site.com"))
{
    request.Cookies = new CookiesCore()
    {
        {"crftoken", "token123"},
        {"session", "12344235"}
    };

    request[HttpHeader.DNT] = "1";
    request["X-Secret-Param"] = "UFO";

    request.AddHeader("X-Tmp-Secret-Param", "42")
           .AddHeader(HttpHeader.Referer, "http://site.com");
    
    // UserAgents can be generated from the Http Class which generates most common used browsers useragents such as chrome.
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

    // if normal
    request.Get("/");
    // if async
    await request.GetAsync("/");
}
```

# Proxies
```csharp
// initializes a proxy client and parses the proxy string to use it
var proxyClient = HttpProxyClient.Parse("127.0.0.1:8080");

// creates the connection to the http server from the proxy set above and return the response of the server.
var tcpClient = proxyClient.CreateConnection("site.com", 80);
// or asynchronously
var tcpClient = proxyClient.CreateConnectionAsync("site.com", 80);
```
### HttpRequest Proxy support
```csharp
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
```
> Will be adding synchronized ProxyClients, until then this will stay here.

# Handling Exceptions
```csharp
try
{
    using (var request = new HttpRequest("site.com"))
    {
        request.Proxy = Socks5ProxyClient.Parse("127.0.0.1:1080");
        request.Get("/");
        // or asynchronously
        await request.GetAsync("/"); 
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
``` 

# Wiki
Refer to the [Wiki](https://github.com/Shadyzpop/Shadynet/wiki/) for the examples and the features.


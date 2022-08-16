

J.E. - 6/5/2017 - 
	Modified original RestSharp source for Silverlight client to NOT register ClientHttp globally.
	Only modify the HttpWebRequest instance used by RestSharp.Silverlight.


BEFORE - 

private HttpWebRequest ConfigureAsyncWebRequest(string method, Uri url)
        
{

#if SILVERLIGHT
            
	WebRequest.RegisterPrefix("http://", WebRequestCreator.ClientHttp);
            
	WebRequest.RegisterPrefix("https://", WebRequestCreator.ClientHttp);

#endif
            
	HttpWebRequest webRequest = (HttpWebRequest) WebRequest.Create(url);



AFTER - 

private HttpWebRequest ConfigureAsyncWebRequest(string method, Uri url)
        
{

#if SILVERLIGHT
            
	//WebRequest.RegisterPrefix("http://", WebRequestCreator.ClientHttp);
            
	//WebRequest.RegisterPrefix("https://", WebRequestCreator.ClientHttp);
            
	HttpWebRequest webRequest = (HttpWebRequest)WebRequestCreator.ClientHttp.Create(url);
#else
            
	HttpWebRequest webRequest = (HttpWebRequest) WebRequest.Create(url);
#endif
#endif
	webRequest.UseDefaultCredentials = this.UseDefaultCredentials;


# Wynathan.Net
A web-/net-oriented library that contains API to interact with web resources 

TODO: copy IMAP, POP, SMTP interaction utils from scom util
TODO: reconsider scom util code to make an API out of it
TODO: provide a separate project that will imitate scom fuctionality (telnet-like client);
	base the new project on the API described above (mail protocol interactions API);
	may be replace scom with the new one created here;
	provided quick access batches
TODO: provide unit tests project
TODO: look up lots of different cases to test HttpClient
TODO: come up with cases to test mail clients (there has to be some stored at OneDrive, for IMAP 
	at least; use stored logs if necessary)
TODO: provide a project setting (compilation constant) and a compilation configuration (both for 
	Debug and Release setups) to allow compiling into a COM-component; provide appropriate classes, 
	wrappers (for ease of use from within, e.g., VBScript, with object properties), copy hell-knows 
	where stored instruction for compiling into a COM-component (should be at OneDrive somewhere), etc.
TODO: avoid using any nuget packets where possible
TODO: perhaps downgrade required .Net Framework version to 4.0
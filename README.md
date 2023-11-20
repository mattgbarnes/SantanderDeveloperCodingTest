# SantanderDeveloperCodingTest

**Assumptions**

-that this is supposed to only be a few hours of development

-some degree of the data being out of date is not critical - this applies both to the top stories and to the details
(if this is not the case, then the cache timeouts can be set to -1 to force a fetch each time)

-that it's possible that the details of a story can change over time

-that if a request is made for more than the number of available stories then it should return all that are available (rather than error)

-that if a request is made for a negative number of stories then an exception should be returned


**To run**

hit f5 and either use the swagger gui or send http requests to whatever port has been assigned


**Next steps**

aside from adding the usual tests and logging, and investigating the subscription to notifications of changes functionality that is alluded to in the docs, and, no doubt, bug-fixing - I'd want to get some specifics on the requirements - e.g. is out of date data acceptable, is the item data to be used elsewhere (would cache that rather than the specific story info), what should the behaviour be if the hackernews website is unavailable (error, return last available), required behaviour if a request comes in whilst a cache refresh is taking place (wait, error, return last available) etc

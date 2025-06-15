# Cursory glance at Cursor

All I seem to see on my Twitter/X feed (aside from copious amounts of Elon-recommended MAGA content) is mega threads from AI exponents claiming you can "just build things" no matter what your previous level of experience. It seems that AI tools have come a long way and AI-assisted software development almost the most of any of these tools.

With [possible redundancy](https://www.thisisoxfordshire.co.uk/news/25219936.job-cuts-considered-nhs-chiefs-amid-oxfordshire-overhaul/) staring me down (thanks Wes), I thought now is the time to throw off my somewhat luddite priors about the capabilities of AI, embrace the cold-hard reality of linear algebra driven agents and try some so-called "vibe coding". 

The tool of choice for serious AI-augmented developers seems to be Cursor. For those not in the know, this is a fork of VS Code with AI magic added. This blog post documents my experience minute by minute with the tool.

## The brief / rules
In my day job, the phrase of the hour is strategic commissioning. Whilst the core of this concept has been around for ages (i.e. World Class Commissioning was a concept when I was still at school) the latest NHS re-organisation has thrown it back into the limelight.

[**TODO**: Include healthcare commissioning wheel from publicly available strat comm documents]

One of the key components of the healthcare commissioning wheel is understanding your population. There is an excellent dataset of public health data called [Fingertips](https://fingertips.phe.org.uk/) from the Department of Health and Social Care.

Wouldn't it be cool if you could just ask natural language questions and get answers based on Fingertips data? I've called it Six Fingertips (ha!) and it needs to allow users to provide natural language queries and get accurate data / analysis out the other end.

## The knowledge
I am not a perfect "vibe coder" as I'm starting from a fair level of understanding with a Computer Science bachelor's degree. I also have some knowledge of the data available in Fingertips.

I wanted a programming language which I haven't personally used in a while so don't have much/any experience of how it works currently. C# will be my language of choice using .NET Core.

I'm also studying for an AI Engineering cert so will make use of Azure's offering of AI services.

## Minute 0-30 (18:40)

* Create an account using Github SSO - very easy
* Editor experience incredibly familiar (nice!)
* Reading the docs - completion, inline and chat (seems sensible)
    * Can get an agent to do some work for you, ask questions about certain parts of code and make custom modes
    * Want the blog post ignored in code base - `.cursorignore` looks like right thing. User specific so also needs to go in `.gitignore`

## Minute 30-60 (19:10)
* Still reading the docs
    * You can choose what context you give models using `@` like you would naturally when chatting
    * Rules allow you to change how the AI models respond - project and user level but also generated through chat
    * Need rules as LLMs don't memorise between completions. Rules are the Cursor way of adding extra prompt content in addition to the code.
* Pricing
    * Based on requests
    * $0.04 (3p) per request
    * Each request is actually several "requests" depending on how expensive the model is
    * I hate that models just have stupid non-intuitive names and versioning.
    * Big name models that I recognise are all there with different pricing. No idea which model to use.
    * Get a budget of "requests" and when its spent they still work but it goes slower
* Want a blank web app using .NET core and C#
    * That's enough reading - who needs that when AI exists
    * Not sure how much effort to put into my initial prompt. Went with the below

    ```
    Please do the following things for me:
    1. Install the latest .NET Core tools
    2. Create a blank web app project in my current directory using C# and .NET Core best practice.

    The basic web app should have a single page with the contents "Hello World".
    ```
    * Weird how we say please isn't it!
    * Doesn't look like I have to select a model - guess it "just works?"
    * It asks me before running commands in my shell. That seems like a sensible security and sanity measure. It starts by checking if .NET is already installed.
    * It's going to wget some source repo packages from some microsoft.com URL and then apt-get install them. Seems sensible. Pressed run again.
    * Would have got stuck on my password. Clicked in the terminal and entered my password. Neat that it lets you enter its terminal like that.
    * OK that just full on didn't work. It's apologised to me and is going to try another script from dot.net. I don't recognise that so Google it - official MS site. Clicked run again.
    * It's going to add a non existant `.dotnet` directory to my path and then create a new webapp apparently. Run.
    * It shows me the diff of the changes its made. Or at least I think so as there's also a "Tick" and "Cross".
    * It's gonna run the app now. Cool let's try that. Run.
    * It's running based on the console output. I manually open URL in browser. OK that worked and it did make the changes.
    * Use old fashioned human brain to git commit progress so far.

## Minute 60-90 

* Came back after a few days
    * No idea how to run this web app - tried the command the AI said and it doesn't work. Going to use "ask" mode.
    * OK it was the AI's fault for not adding the installed dotnet to path
    * I'm not convinced it installed it in the best way if I'm having to do that but there we go
    * Wanted to rename - ran the commands and it broke. Tried deleting things using human brain. Didn't work.
    * Asked AI to fix its own mess - it gave a solution (to rename to be explicit about namespace) but I worked out a better one - change the view imports. Take that AI.
    * Want to fix .gitignore so asked the AI to do it. All ignored correctly.
* Let's get it to create the user interface
    * I think I'm going to get AI to do it in stages like I would
    * Went with
    ```
    Modify @/SixFingertips web app to modify the front page to have a single large text box and a submit button which calls a function.
    ```
    * It's actually quite cool that this works.
    * OK wonder if it will do all the styling. I want it to use NHS design system
    ```
    Restyle the app to use the NHS.uk frontend library. Use NHS.uk frontend components to replace bootstrap components. Documentation on how to import the relevant parts are at @https://service-manual.nhs.uk/design-system/production 
    ```
    * For some reason it's used version v7.1.0 and when I check the latest is v9.6.2
    * Let's see how it looks - it looks pretty good. The footer is a bit broken though. I wonder if we can fix that.
    ```
    Can you fix the footer to use the footer component as intended?
    ```
    * Let's ask it to use the latest version instead. Those who don't know what they're doing will definitely get old stuff or the wrong stuff.
    * It downloaded it but made the wrong imports I think. Changed them. Still broken. Asked AI to fix it.
    ```
    The styling is broken - the menu and footer don't look right.
    ```
    * Let's see if it can revert back the changes it made
    ```
    It is still broken. Revert the changes back to before you changed from v7.1.0
    ```
    * It works again. We'll try again now. Will it avoid fucking it up this time?
    ```
    Update the version of nhsuk-frontend to v9.6.2 from v7.1.0. Update any component code as needed to ensure it looks the same after the version change.
    ```
    * It starts by barking up the wrong tree and looking for an automatically imported package. It can identify the changes needed but instead of doing them, like it did before. It stops part of the way through and asks me if I'd like more help.
    ```
    Please download and replace the old files with the new files
    ```
    * It's not found the right place to download them this time. It's getting frustrating. I'm just going to help it out and do it myself.
    * It's broken in exactly the same way. Guess there were breaking changes between v7.1.0 and v9.6.2 it didn't identify.
    ```
    The header doesn't look right and has an out of place "Menu" text. Can you fix it to use the correct NHS design system header?
    ```
    * It hasn't fixed anything. I'll try asking in a different way. I'm starting to find this frustrating. I could have done this much more quickly myself.
    ```
    Remove the text and button that says "Menu"
    ```
    * It's removed the whole navigation section now...
    ```
    I didn't ask you to remove the whole navigation bar. Put the Home and Privacy Policy links back.
    ```
    * We did it Joe! Now to fix the footer again.
    ```
    Fix the footer now. Do not change any of the links, just fix the styling to use the correct NHS Design System styling.
    ```
    * Hmm it's still broken
    ```
    It's still not fixed. Look again to ensure the footer component is being used correctly.
    ```
    * It's still broken, so let's try prompting a bit differently.
    ```
    Remove the footer entirely. Insert a new NHS Design System footer component.
    ```
    * I give up.
    ```
    Remove the whole footer
    ```
    * I'm having to rebuild every time - I wonder if there's a live watch style way of doing this. AI gave the right answer (`dotnet watch`) first time with explanation.
## Minute 90-120

* It's time to get meta. I don't think I'm using the tool effectively. I'm going to ask Cursor how to prompt Cursor effectively.
    * It looks like clarity and context is key. Somebody on Twitter (I got distracted) has described it as a junior dev with amnesia, which sounds about right.
    * It's almost like you have a highly skilled dev but they need a micromangement level of direction
    * No wonder my vague instructions about the footer didn't work. I wonder whether I need to provide the documentation up front.
    ```
    I am creating a .NET core web app in @/SixFingertips using the NHS Design System. Carry out the following steps:
    1. Update the site-wide page template to match the "Content Page Template" on @https://service-manual.nhs.uk/design-system/styles/page-template
    2. Remove the search box
    ```
    * That worked a LOT better.
* Big leagues time now. I'm going to get it to help me decide between Terraform and Azure ARM Templates
    * It's recommended Terraform. Let's try using my learning to see if I can give it a good prompt and get it going
    ```
    I am writing a web app which will allow users to ask questions to an AI agent connected to a third party service. Plesae perform the following steps - perform these strictly in order and do not skip any steps:
    1. Install Terraform if it is not already installed
    2. Create a new Terraform project inside a new top level project folder called "SixFingertipsInfrastructure"
    3. Write Terraform code to deploy an Azure AI Foundry Agent that uses GPT-4o-mini
    ```
    * It's produced some feasible looking Terraform with some minor redundant code and some names I think could be clearer. I tidy up after it.
    * Let's let rip - `terraform plan` seems sensible so I apply it. I get an error on apply.
    * I ask the model for a correction. It tells me my model name is wrong. I don't believe it. I look at the docs and come to a different answer. It works this time.
    * I'm not sure if it's just me but I'm having to do a fair bit of babysitting and keep my eye on the ball to tidy up errors. Perhaps I'm using the wrong models, perhaps I'm not providing enough context. Next time I am going to add way more.
* I think it's time to get it to write some backend code. I'm going to use a huge prompt and give very explicit instructions. Let's see if that does any better.
    ```
    You are an AI agent writing a .NET core web app in C# (code in @/SixFingertips ) which allows the user to interact with an AI agent.

    The objective is to create a working interface so that when the user inputs text into the main textarea and clicks Submit in @Index.cshtml, the input is submitted to an agent and the answer displayed on the same page.

    DO NOT finish until the objective is complete. Complete the objective by completing ALL of the steps below:
    1. Study the documentation for the Azure.AI.Agents.Persistent package at @https://learn.microsoft.com/en-us/dotnet/api/overview/azure/ai.agents.persistent-readme?view=azure-dotnet 
    2. Install the package to the project
    3. Create code that instantiates an agent using an already deployed Azure OpenAI Services resource (allow deployment details to be input via environment variables or similar)
    4. Allow the user to submit input to the agent using the textarea and submit button
    5. Display the messages returned by the agent on the main page
    6. Ensure the user interface uses NHS Design System components
    ```
    * A common theme is emerging. Given knowledge cut off, it does seem to struggle with getting the right package versions. I'm getting build errors and security vulnerabilities.
    * What I am really enjoying is the "Ask" function which definitely saves time versus looking in the documentation. This is especially true given my shaky understanding of .NET Core.
    * Getting build errors still, but at least they're not package related.
    * I'm going to get the agent to fix these errors.
    ```
    There are some build errors. Please correct the build errors and verify the code builds correctly. Continue until you have fixed the code so it builds correctly.
    ```
    * More errors. Let's try chucking the context and pretend I made the mistakes. Now there's modesty.
    ```
    I am building a web app in @/SixFingertips using .NET Core in C# and the Azure AI Persistent Agents library.

    I appear to have made some mistakes whilst creating the code and need you to fix them. The build errors I am getting are provided as context.

    You may need to use a web search to find relevant documentation, or navigate links off the documentation homepage for the library@https://learn.microsoft.com/en-us/dotnet/api/azure.ai.agents.persistent?view=azure-dotnet
    ```
    * Still not quite working, we roll again. I realise I feel quite vulnerable handing over all this control to AI as I don't really fully understand anything its doing. I also feel like I'm not learning anywhere near as much from the process as I'm not understanding what it's doing or fixing things. 
    ```
    There is one more issue to fix. I have provided the build error. Please provide a fix that enables the code to build.
    ```
    * In any case simply adding a new package hasn't fixed things and doesn't give me a good gut feel. I'm going to reject those changes and try again.
    * This time I use my human brain to find the right AI documentation for the line of code causing the error. I use the inline function and tell the AI what mistake it's made and give it the documentation link to use to try and find the right methods.
    * New build error. I fust around in the docs and make some manual changes to get the errors to go away. I don't know what I'm doing but I have at least got it to build. Let's see the vibe.
    * LOL I forgot to add my credentials. Let us try again.

## Minute 120-150
* It's added some quite good error handling at least. But the code is very much still broken.
    * I think it's actually the infrastructure that isn't quite working and it's no fault of the agent that did the backend code. I tried to use an Azure AI project I'd set up earlier to give it a model to call and work with.
    * Now there are new errors. It's really really struggling with this to be honest. Neither the inline code editing chat, nor tab suggestions yield correct or valid code, even with prompting.
    * I really hate the automatic tab suggestions in Cursor. I'd happily accept the suggestions if they were any good but every time I've tried to accept them it's yielded garbage. What I really want to do is accept the "legacy" suggestion from the interface. 
    * Given I don't have any documentation pop ups myself I wonder if the lack of type information is helping or hindering it. The lack of documentation hoverovers are really hindering me for sure.
    * Maybe it's because I ignored a prompt to install an extension. Yeah the extension isn't installed for C# - doh.
    * AI is still unable to write async code after installing the code, but then again so am I as I have no clue how it works in C#. Luckily with the extension enabled I am now able to guess my way through the methods to get something working.
    * After much pain and suffering I got it to work.
* I think it really struggles to use its own knowledge if the library you want to use didn't exist at the knoweledge cut off date. I wonder if some very targeted prompting will help it do better for the next task. It certainly seems adept at using the .NET core components but it makes mistakes I don't make even with complete "most likely next suggestion" guesswork.

# Minute 150-180
* After consulting docs it seems that what I need doesn't have Terraform helpers that can create it. So I Terraform destory things and delete the folder. Nothing ventured nothing gained.
* I want to refine the user interface so I try and get it to add a simple generating message whilst the agent is thinking
    ```
    You are editing a .NET core app which connects to an Azure AI Agent. 

    Currently when the user clicks the submit button, nothing is displayed in the user interface whilst a response is being generated.

    Please add an informative message to the screen which is displayed only whilst waiting on the AgentService to generate the result.
    ```
    * It has an attempt, but that attempt uses JQuery, which does not exist in the project and the JavaScript just fails to function. Clear the context. Try again with a new prompt.
    ```
    Make edits to @Index.cshtml and @Index.cshtml.cs so that an informative message is displayed to the user between clicking "Submit" and the result being displayed.
    ```
    * That also didn't work. It's trying to change the page display using server side code only, but the issue is the page doesn't do anything whilst waiting for a post response
    * Maybe it's using a dud model. What if I specifically task Claude with the job? It's going to cost more requests but mucking around as-is is costing a huge amount of requests.
    ```
    Make edits to @Index.cshtml ONLY which display an informative message between clicking on the "Send" button and the page completing loading the response once generated.

    Use only pure JavaScript. Do not assume any libraries are installed.
    ```
    * With a bit more direction it succeeds. Not sure the manually specified model makes any difference though. This seems to be a theme!
* The agent often returns Markdown, but we want that displayed in the interface as HTML. Time to ask an Agent to help us out...
    ```
    I am creating a .NET core app in @/SixFingertips which allows the user to enter a prompt and have an AI agent respond.

    The response String that is returned by the agent in the highlighted line in @Index.cshtml.cs often contains Markdown text.

    Add processing logic to @Index.cshtml and/or @Index.cshtml.cs which processes any Markdown returned and displays the information back to user with correct formatting.
    ```
    * OK, I'm getting way better at this. That just worked first time.
    * Sometimes it feels like I'm a superhero with the AI when it works and sometimes I just feel stupid and supernumerary.
* Now I want to get the agent to use Fingertips data so I grab the API spec, convert from Swagger v2 to OpenAPI v3 and dump into my project folder.
    * Time to use AI to integrate the tool
    ```
    I am writing an AI agent to help users make use of data from the Fingertips public health dataset. 

    The agent is implemented using the Azure AI Foundry Agent library (documented at @https://learn.microsoft.com/en-us/dotnet/api/overview/azure/ai.agents.persistent-readme?view=azure-dotnet#function-call )

    Using the example in @https://learn.microsoft.com/en-us/azure/ai-services/agents/how-to/tools/openapi-spec-samples?pivots=csharp , modify ONLY code in @AgentService.cs  to add an OpenApiToolDefinition that uses the OpenAPI spec defined in "wwwroot/fingertips_api_spec.json"
    ```
    * Again, worked first time! I suspect I need to improve the prompt because it's getting stuck using the (admittedly very complicated) API for Fingertips.
# Minute 180-...

* Now it's time to refine the design and functionality of the app before readying this blog post for publication
* The prompt to the agent clearly needs work as it fails to understand how to use the Fingertips API
    * To be fair to it, I don't currently understand how to use the Fingertips API
    * This is an area I know that I will now need to understand better to augment the context provided to the AI to help it solve more problems
    * I need a way of testing if I'm making the agent better or worse, so I design a set of test cases in a .csv file.
    * OK so I needed to understand the API a bit better to understand what sorts of questions that can and can't be answered before writing my CSV
    * Doing this as a human I'm learning things. For example the API uses the NHS ODS codes for some thigns (e.g. GP practices, NHS Trusts) but not others (e.g. ICBs and CCG boundaries which get ONS style E.... codes)
    * Turns out you can trace what your agent is doing if you try it out on the Azure AI Foundry portal. This helps me see what API calls its trying before failing.
    * It appears that the agent gets confused by the overwhelming choice of methods to call to find an area. 
    * I've cut down the different functions the agent can use to a single function by manually curating a subset of 1 function of the whole OpenAPI spec. I have also modified some of the documentation to explain better in plain English what that method does.
    * I plan to slowly add back in methods and tweak the documentation to improve performance. Before that I'd better write a few test queries and then I can report back here on the score for each iteration.
    * Turns out there's a lot of things in Fingertips (who guessed!) so I'm going to write test prompts that target correct answers about GP practices and their related entities.
    * Wrote some test cases that should be enough to do a little fine tuning as this is just an experimental project. Tested with the first agent which had the full API first as "v0" and stored the responses in the spreadsheet with the prompts.
    * Version 0 passes only 1 out of 11 test cases - a paltry 9%.
    * I've further tweaked the documentation and provided a restricted list of the area methods to help the agent get the right method first time. This is version 1 and should do a lot better at the various area questions in my test set.
    * The new version now passes 7 out of 11 test cases - a vast improvement. In both versions the LLM displays some weird behaviour - pretending it could do it if the system worked properly or just lying.
    * The oddest thing about this, is it hasn't really been code I've been editing, its been prompts and documentation. If AI is to take off, we must curate clearly written information and docmentation to help it understand the world.
    
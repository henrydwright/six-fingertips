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
    * I'm not sure if it's just me but I'm having to do a fair bit of babysitting and keep my eye on the ball to tidy up errors. Perhaps I'm using the wrong models, perhaps I'm 
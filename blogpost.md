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
    * Would have got stuck on my password. Clicked in the terminal and entered my password. Neat.
    * OK that just didn't work. It's apologised to me and is going to try another script from dot.net. I don't recognise that so Google it - official MS site. Clicked run again.
    * It's going to add a non existant `.dotnet` directory to my path and then create a new webapp apparently. Run.
    * It shows me the diff of the changes its made. Or at least I think so as there's also a "Tick" and "Cross"
    * It's gonna run the app now. Cool let's try that. Run.
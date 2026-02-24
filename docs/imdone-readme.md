## #TODO :green_circle: Start Here: Press **V** to view this card!

Each card is a task that will help you get familiar with imdone.

If you'd rather just start using imdone, you can add your own project by pressing **command** or **control** + **N**.

<!-- The {{icons.kebab}} is a property that is set in the custom properties file .imdone/properties/card.js -->
You can also open the main menu by clicking the {{icons.kebab}} on your top left and click the ***Add a project*** menu item.

### Moving cards

1. If you're still in the focus view, move this card to **DOING** by pressing **shift** + :arrow_right: or change the list to **DOING** in the dropdown to the right.
2. Go back to the board by pressing **esc** or clicking the **X** above and to the right.
3. Now move this card to **DONE** by dragging it to the DONE list or pressing **shift** + :arrow_right:.

*board view* ![board view](./images/board-view.png)
*focus view* ![focus view](./images/focus-view.png)

<!--
#getting-started -- This is a tag that can be used to filter cards
#urgent -- This is a tag that can be used to filter cards
remind:2026-02-23T15:40:01-06:00 -- This is remind metadata
due:2026-02-23T15:43:01-06:00 -- This is due metadata
created:2026-02-23T15:38:00-06:00 -- This is when the card was created
order:0 -- This is metadata that determines the card priority order
-->


## #TODO :gear: Configure which apps to open files with!

This board is built-in with your code and markdown files which makes it easy to get started on the work and keep context while getting things done.

You can configure which apps you want to open your files with in the project settings.

- Press **command + /** on mac or **control + /** on windows to open the project settings
  - You can also click the {{icons.kebab}} (Menu) button in the upper left corner and click **Settings** -> **Project Settings**
- Click the **Open Files With** dropdown and select your favorite editor or enter a command that will open it to the line in the file where the card is defined.
  - To open files in Obsidian your project folder must also be an obsidian vault. 
  - Watch [this 5 minute video](https://www.loom.com/share/52b25017e13d4a2a8cea221f7bbc95bb?sid=df2c4492-8ef9-4eba-86c9-129bdd9d7c5d) or read the [Setup Instructions for the "Open cards in imdone" Obsidian Plugin](https://github.com/imdone/imdone-obsidian-plugin?tab=readme-ov-file#setup-instructions-for-the-open-cards-in-imdone-obsidian-plugin) to get started.
- Click the **Open Code Files With** dropdown and select your favorite code editor.
- Click **Save** to save your changes.

Learn more about opening editors [here](https://imdone.io/docs/#/editors)

<!--
#getting-started
#important
kudos:true
remind:2026-02-23T15:48:01-06:00
due:2026-02-23T15:50:01-06:00
created:2026-02-23T15:38:00-06:00
order:20
-->


## #TODO :file_cabinet: Go to the card source file

1. Select this card and press **command + O** on mac or **control + O** on windows to open the card source file directly to the first line of the card.
2. When you update the card in the source file, the changes will be reflected in imdone.
3. Take a look at the comment below to get familiar with tags and metadata.

<!--
#getting-started -- This is a tag that can be used to filter cards
remind:2026-02-23T16:08:01-06:00 -- This is remind metadata
due:2026-02-23T16:10:01-06:00 -- This is due metadata
created:2026-02-23T15:38:00-06:00 -- This is when the card was created
order:40 -- This is metadata that determines the card priority order
-->


## #TODO :pencil2: Edit a card in Imdone

1. If your in the board view, double click this card or press the **E** key while it's selected to edit it.
2. Make some changes and save them by pressing **command + S** on mac or **control + S** on windows.
3. Press **esc** to close this editor and view this card in the focus view.

<!--
#getting-started
kudos:true
remind:2026-02-23T17:38:01-06:00
due:2026-02-23T17:40:01-06:00
created:2026-02-23T15:38:00-06:00
order:60
-->


## #TODO :heavy_plus_sign: Add a new card

1. Press the space bar or click the **+ Add a card** button at the bottom of a list.
2. Add some content to the card and press **command + S** on mac or **control + S** on windows to save it.
3. Press **esc** to close the editor.

**Remember:** You can create a new card directly from your code files by adding a comment with a tag like `TODO` or `NOTE`. It's the same for markdown, just add a list name tag like `#TODO` or `#NOTE` at the start of the card title, the following lines will be the card content. The card content ends when 2 blank lines or another list name tag is found.

<!--
#getting-started
remind:2026-02-24T09:00:00-06:00
due:2026-02-24T09:00:00-06:00
created:2026-02-23T15:38:00-06:00
order:80
-->

## #TODO :mag: Filtering tasks in Imdone

Imdone allows you to filter your board to focus on what matters. Here’s how you can use filters:  

Filter your board by pressing `command + F` on Mac or `control + F` on Windows and entering the filter.

### **Hide specific lists**  
To hide the `TODO` list, use this [filter]({{getFilterURL('hide: TODO')}}): <!-- This is how you can get a filter link in your board -->
```markdown
hide: TODO
```

**note:** You can also hide multiple lists by separating them with a comma.

### **Filter by tag**  
To display only [tasks tagged with **urgent**]({{getFilterURL('tags: urgent')}}), use:
```markdown
tags: urgent
```

### **Find tasks assigned to you**  
If tasks have an assignee field, filter [tasks assigned to **john**]({{getFilterURL('meta.assignee: john')}}):
```markdown
meta.assignee: john
```

### **Search by keyword**  
To find [tasks containing the word **filter**]({{getFilterURL('filter')}}), use:
```markdown
filter
```

### **Combine filters**
You can combine filters to narrow down your search. For example, to find [tasks tagged with **urgent** and assigned to **john**]({{getFilterURL('tags: urgent and meta.assignee: john')}}), use:
```markdown
tags: urgent and meta.assignee: john
```

<!-- 
#getting-started #urgent
assignee:john
remind:2026-02-24T10:00:00-06:00
due:2026-02-24T10:00:00-06:00
created:2026-02-23T15:38:00-06:00
order:100
-->

## #TODO :link: Use filter URL variables in your cards

Learn how to use Imdone's filter URL variables for dynamic links in your cards.

### :mag: What They Do

- **`{{getFilterURL(filter)}`** – Inserts a URL that applies the given filter.
- **`{{getFilterLink(filter)}`** – Inserts a Markdown link that applies the filter, using the filter as the link text.
- **`{{clearFilterURL}`** – Inserts a URL that clears the filter.

### :hammer_and_wrench: How to Use Them in Cards

#### Getting a Filter URL

----

##### Example

> View all TODO tasks [here]({{getFilterURL('list:TODO')}}).

##### Markdown

```markdown
View all TODO tasks [here]({{getFilterURL('list:TODO')}}).
```

#### Creating a Filter Link

----

##### Example

> See all TODO tasks: {{getFilterLink('list:TODO')}}

##### Markdown

```markdown
See all TODO tasks: {{getFilterLink('list:TODO')}}
```

#### Clearing Filters

----

##### Example

> Click [here]({{clearFilterURL}}) to clear all filters.

##### Markdown

```markdown
Click [here]({{clearFilterURL}}) to clear all filters.
```

### :white_check_mark: Try It Out!

- Add a **clear filter link** to a card using `{{clearFilterURL}}`.
- Create a **DOING filter URL** with `{{getFilterURL('list:DOING')}}`.
- Generate a **DOING filter link** with `{{getFilterLink('list:DOING')}}`.

:rocket: Now, test these in your Imdone board and watch them work dynamically!

<!--
#getting-started
#urgent
remind:2026-02-24T10:00:00-06:00
due:2026-02-24T10:00:00-06:00
created:2026-02-23T15:38:00-06:00
order:120
-->

## #TODO :computer: Extend imdone with javascript and css

You can create custom properties and actions for your project using javascript.

There are board actions and card properties included in this tutorial project. To get an idea of what you can do, open the extension files from the main menu under **Extension Files** or use these links.

[Board Properties](.imdone/properties/board.js)
[Board Actions](.imdone/actions/board.js)
[Card Properties](.imdone/properties/card.js)

Check out the [Extensions documentation](https://imdone.io/docs/#/extensions) for more info.

If you're interested in building a plugin, check out the [imdone/imdone-sample-plugin: A sample plugin for Imdone with some pretty useful features!](https://github.com/imdone/imdone-sample-plugin)

<!--
#getting-started
created:2026-02-23T15:38:00-06:00
kudos:true
remind:2026-02-25T15:38:01-06:00
due:2026-02-25T15:38:01-06:00
order:140
-->


## #TODO :bulb: Use custom templates to add card content

1. Press **space** or click the **+ Add a card** button at the bottom of a list.
2. Tab past the file field and add a title to the card on the first line in the editor.
3. Type `${template_user_story}` on the second line and press **command + S** on mac or **control + S** on windows to save the card.
4. Press **esc** to close the editor.

You can add templates or modify existing ones by updating the markdown files in the [*.imdone/templates*](./.imdone/templates) folder.

<!-- 
#getting-started
created:2026-02-23T15:38:00-06:00
remind:2026-02-26T09:00:00-06:00
due:2026-02-26T09:00:00-06:00
order:160
-->

## #TODO :white_check_mark: Complete a subtask

1. Click the box below.
    - [ ] check me
2. Press **command + O** on mac or **control + O** on windows to open the card source file directly to the first line of the card.
  - Notice the x in the box
  - You just completed a subtask!

<!--
#getting-started
kudos:true
created:2026-02-23T15:38:00-06:00
remind:2026-02-26T10:00:00-06:00
due:2026-02-26T10:00:00-06:00
order:180
-->

## #TODO :bell: Set a reminder!

Just add a line to your card that starts with remind and ends with a period.

**Like this!**

`Remind me tomorrow at 6pm.`

**If you're lazy do this... :zzz:**

`remind tomorrow 6pm.`

<!--
#getting-started
created:2026-02-23T15:38:00-06:00
remind:2026-02-27T09:00:00-06:00
due:2026-02-27T09:00:00-06:00
order:200
-->


## #TODO :partying_face: Celebrate when you complete a task

Add `kudos:true` to a card to celebrate when you complete it.

You can also set the **Kudos probability** in the project settings to celebrate randomly.

<!--
#getting-started
kudos:true
created:2026-02-23T15:38:00-06:00
remind:2026-02-27T14:00:00-06:00
due:2026-02-27T14:00:00-06:00
order:220
-->


## #TODO :control_knobs: Check out the settings

1. Press **command + O**  on mac or **control + O** on windows to open the project settings.

<!--
#getting-started
created:2026-02-23T15:38:00-06:00
remind:2026-02-28T11:00:00-06:00
due:2026-02-28T11:00:00-06:00
order:240
-->


## #TODO :arrow_right: Navigate to this card with the arrow keys

1. Move this card to **DOING**
2. Use the arrow keys to navigate to this card.
2. Press `V` to open the card in focus mode.
3. Navigate to the next card with the left arrow key, then navigate back with the right arrow key.
3. Press `esc` to close the card.

<!--
kudos:true
created:2026-02-23T15:38:00-06:00
remind:2026-03-01T15:00:00-06:00
due:2026-03-01T15:00:00-06:00
#getting-started
order:260
-->


## #TODO :tshirt: Press the `T` key to toggle tags, metadata and sub-task progress

<!--
#getting-started
created:2026-02-23T15:38:00-06:00
remind:2026-03-02T09:00:00-06:00
due:2026-03-02T09:00:00-06:00
order:280
-->


## #TODO :keyboard: Set custom keyboard shortcuts

Set custom keyboard shortcuts by going to **Menu** --> **Settings** --> **Global and Default Settings** -> **Keyboard Shortcuts**

<!--
#getting-started
#help
kudos:true
created:2026-02-23T15:38:00-06:00
remind:2026-03-02T17:00:00-06:00
due:2026-03-02T17:00:00-06:00
order:300
-->


## #TODO :smile: Add emoji to your cards using the GFM emoji

:rocket: <span style="font-size: 1.5em;">:rocket:</span> <span style="font-size: 2em;">:rocket:</span> :crescent_moon: 

<!--
#getting-started
#fun
created:2026-02-23T15:38:00-06:00
remind:2026-03-03T14:00:00-06:00
due:2026-03-03T14:00:00-06:00
order:320
-->


## #TODO :scissors: Copy code snippets

Hover over the code and click the {{icons.clone}} to copy it to your clipboard.

```json
{
  "testing": "imdone"
}
```

You can also copy `inline code` by clicking it.

<!--
#getting-started
kudos:true
created:2026-02-23T15:38:00-06:00
remind:2026-03-04T09:00:00-06:00
due:2026-03-04T09:00:00-06:00
order:340
expand:1
-->


## #TODO :sunglasses: Ignore files

To ignore files add an entry in [.imdoneignore](.imdoneignore)
`.imdoneignore` follows the same syntax as `.gitignore`
[Git - gitignore Documentation](https://git-scm.com/docs/gitignore)

<!--
#getting-started
created:2026-02-23T15:38:00-06:00
remind:2026-03-05T11:00:00-06:00
due:2026-03-05T11:00:00-06:00
order:360
-->


## #TODO :grey_question: Get help

Open the help menu by pressing **shift** + **/**. 

Here you'll be able to search for keyboard shortcuts and find links to the imdone documentation.

**If you have any questions, just ask!**

- [Send me an email: jesse@imdone.io](mailto:jesse@imdone.io)
- [Get the Newsletter](https://sendfox.com/piascikj)
- [Watch imdone videos on youtube](https://www.youtube.com/@JessePiascik)
- [Follow imdone on twitter](https://twitter.com/imdoneio)
- [Join the imdone discord](https://discord.gg/b5UQ8HD2hy)
- [Jesse on drift](https://drift.me/jesse36)

<!--
#getting-started
#community
kudos:true
created:2026-02-23T15:38:00-06:00
remind:2026-03-07T09:00:00-06:00
due:2026-03-07T09:00:00-06:00
order:380
-->


## #NOTE :chart: Activity

| Status                 | #                                           | <!--[ {{emoji.chart}} ]-->  |
|------------------------|---------------------------------------------|-----------------------------|
| **What's Due?**        | <!--[ {{totals["What's Due?"]}} ]-->        | <!--[ {{emoji.due}} ]-->    |
| **Recently Completed** | <!--[ {{totals["Recently Completed"]}} ]--> | <!--[ {{emoji.recent}} ]--> |
| **WIP**                | <!--[ {{totals["DOING"]}} ]-->              | <!--[ {{emoji.wip}} ]-->    |

----

:eyeglasses: **[Focus]({{getFilterURL('list = DOING or tags = focus or (list = TODO and index = 0)')}})**
:large_blue_circle: **[Clear filter]({{clearFilterURL}})**

----

:newspaper: **[Get the Newsletter](https://sendfox.com/piascikj)**
:movie_camera: **[Watch imdone videos on youtube](https://www.youtube.com/@JessePiascik)**
:telephone: **[Join the imdone Discord](https://discord.gg/b5UQ8HD2hy)**
:loudspeaker: **[Leave a Testimonial](https://senja.io/p/imdone/r/f9ZPAI)**
:email: **[Send me an email: jesse@imdone.io](mailto:jesse@imdone.io)**
:x: **[Follow imdone on X](https://twitter.com/imdoneio)**

<!--
#focus
#getting-started
expand:1
order:0
-->



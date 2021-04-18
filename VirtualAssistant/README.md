# PhaticChatBot_Telegram
### Chat bot which maintains phatic dialogue with user.

## Guide
**For users:**<br>
**Just search in telegram `@PhaticChatBot`or just go to [PhaticChatBot](https://t.me/PhaticChatBot)**<br>
**Start conversation with him.**<br><br>
**For owners:** <br>
Chat bot has some commands:
+ `/help` - helping information
+ `/info` - short information about storage content
+ `/add <storage key> <word types to add> <word to add>`<br> - adding new words(only words, not sentences patterns) to storage(not working yet)<br>This commands is supposed to simplify `patterns.json` updating
+ `/metric <metric name>` - used to display some metrics, for example cpu use percentage, available RAM etc.<br>
Command without parametr will display possible parametrs

<br>
If you want to modify sentences pattrens you have to write manually in `patterns.json`.

Example of pattern 
```json
"Statements" : {
    ">p (i|you) >v >a *" : ">r >v >r >p really >a * ?",
}
```
**The key of the element means which pattern is expected, the value means how to generate answer**<br>
**For example here fits phrase `I am a programmer`, and the answer will be `Are you really a programmer ?`**
<br>
<br>
`>p` after `>` goes key token which means that in this place could be any pronoun which bot knows.
Similarly `>v` - verb, `>a` - article <br> 
`>r` means "replace next word with a word from Replacements" For example it's useful when you want to change **you -> i** and etc. <br>
`*` means any other word <br>
also after key token you can specify in brackets which words could be here <br>
For example: in `">p (i|you) >v >a *"` in pronoun place could be only `i` or `you`<br><br>
**Remember:**<br>
 :arrow_double_down:
Patterns finder reads object from top to the bottom <br> 
```json
"Statements" : {
   ">p (i|you) >v >a *" : ">r >v >r >p really >a * ?",
   ">p >v >a *" : ">v >p really >a * ?",
}
```
:arrow_double_down:
which means that you have to place more _**specific patterns higher**_ and _**general ones - lower**_ <br>
## Libs, frameworks and deployment<br>
**Frameworks:** _**.NET Framework 4.7.2, ASP.NET 3.0**_ <br>
**Libs:**  _**[Porter2Stemmer](https://github.com/nemec/porter2-stemmer),  [Telegram.Bot](https://github.com/TelegramBots/Telegram.Bot)**_<br>
**Deployed to _Microsoft Azure_**<br>
**Interaction with Telegram through _webhooks_**

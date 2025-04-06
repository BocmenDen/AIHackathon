﻿global using BotCore.Attributes;
global using BotCore.Models;
global using CommandFilter = BotCore.FilterRouter.Attributes.CommandFilterAttribute<AIHackathon.DB.User>;
global using HandleFilterRouter = BotCore.FilterRouter.HandleFilterRouter<AIHackathon.DB.User, BotCore.Interfaces.IUpdateContext<AIHackathon.DB.User>>;
global using HandlePageRouter = BotCore.PageRouter.HandlePageRouter<AIHackathon.DB.User, BotCore.Interfaces.IUpdateContext<AIHackathon.DB.User>, string>;
global using IDBUserPageKey = BotCore.PageRouter.Interfaces.IDBUserPageKey<AIHackathon.DB.User, string>;
global using IDBUserPageModel = BotCore.PageRouter.Interfaces.IDBUserPageModel<AIHackathon.DB.User>;
global using IDBUserPageParameter = BotCore.PageRouter.Interfaces.IDBUserPageParameter<AIHackathon.DB.User>;
global using IPage = BotCore.PageRouter.Interfaces.IPage<AIHackathon.DB.User, BotCore.Interfaces.IUpdateContext<AIHackathon.DB.User>>;
global using IPageOnExit = BotCore.PageRouter.Interfaces.IPageOnExit<AIHackathon.DB.User, BotCore.Interfaces.IUpdateContext<AIHackathon.DB.User>>;
global using MessageSpam = AIHackathon.Services.MessageSpam<AIHackathon.DB.User, BotCore.Interfaces.IUpdateContext<AIHackathon.DB.User>>;
global using PageAttribute = BotCore.PageRouter.Attributes.PageAttribute<string>;
global using PageCacheableAttribute = BotCore.PageRouter.Attributes.PageCacheableAttribute<string>;
global using UpdateContext = BotCore.Interfaces.IUpdateContext<AIHackathon.DB.User>;

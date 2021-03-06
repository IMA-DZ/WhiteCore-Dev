﻿/*
 * Copyright (c) Contributors, http://whitecore-sim.org/, http://aurora-sim.org
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the WhiteCore-Sim Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System.Collections.Generic;
using OpenMetaverse;
using WhiteCore.Framework.Servers.HttpServer.Implementation;
using WhiteCore.Framework.Services;
using WhiteCore.Framework.Utilities;

namespace WhiteCore.Modules.Web
{
    public class UserSearchPage : IWebInterfacePage
    {
        public string [] FilePath {
            get {
                return new []
                           {
                               "html/user_search.html"
                           };
            }
        }

        public bool RequiresAuthentication {
            get { return true; }
        }

        public bool RequiresAdminAuthentication {
            get { return false; }
        }

        public Dictionary<string, object> Fill (WebInterface webInterface, string filename, OSHttpRequest httpRequest,
                                               OSHttpResponse httpResponse, Dictionary<string, object> requestParameters,
                                               ITranslator translator, out string response)
        {
            response = null;
            var vars = new Dictionary<string, object> ();
            var usersList = new List<Dictionary<string, object>> ();
            var IsAdmin = Authenticator.CheckAdminAuthentication (httpRequest);

            string noDetails = translator.GetTranslatedString ("NoDetailsText");

            if (requestParameters.ContainsKey ("Submit")) {
                IUserAccountService accountService = webInterface.Registry.RequestModuleInterface<IUserAccountService> ();

                string userName = requestParameters ["username"].ToString ();
                // allow '*' wildcards
                var username = userName.Replace ('*', '%');

                // check for admin wildcard search
                if ((username.Trim ().Length > 0) || IsAdmin) {
                    var users = accountService.GetUserAccounts (null, username, null, null);
                    var searchUsersList = new List<UUID> ();

                    if (IsAdmin)        // display all users
                    {
                        foreach (var user in users) {
                            searchUsersList.Add (user.PrincipalID);
                        }

                    } else             // only show the users friends
                    {

                        UserAccount ourAccount = Authenticator.GetAuthentication (httpRequest);
                        if (ourAccount != null) {
                            IFriendsService friendsService = webInterface.Registry.RequestModuleInterface<IFriendsService> ();
                            var friends = friendsService.GetFriends (ourAccount.PrincipalID);
                            foreach (var friend in friends) {
                                UUID friendID = UUID.Zero;
                                UUID.TryParse (friend.Friend, out friendID);

                                if (friendID != UUID.Zero)
                                    searchUsersList.Add (friendID);
                            }
                        }
                    }
                    if (searchUsersList.Count > 0) {
                        noDetails = "";

                        foreach (var user in users) {
                            if (!searchUsersList.Contains (user.PrincipalID))
                                continue;

                            if (Utilities.IsSystemUser (user.PrincipalID))
                                continue;

                            usersList.Add (new Dictionary<string, object> {
                                { "UserName", user.Name },
                                { "UserID", user.PrincipalID },
                                { "CanEdit", IsAdmin }
                            });
                        }
                    }

                    if (usersList.Count == 0) {
                        usersList.Add (new Dictionary<string, object> {
                            { "UserName", translator.GetTranslatedString ("NoDetailsText") },
                            { "UserID", "" },
                            { "CanEdit", false }
                        });
                    }
                }
            }


            vars.Add ("NoDetailsText", noDetails);
            vars.Add ("UsersList", usersList);
            vars.Add ("UserSearchText", translator.GetTranslatedString ("UserSearchText") + (IsAdmin ? "" : " (Friends)"));
            vars.Add ("SearchForUserText", translator.GetTranslatedString ("SearchForUserText"));
            vars.Add ("UserNameText", translator.GetTranslatedString ("UserNameText"));
            vars.Add ("Search", translator.GetTranslatedString ("Search"));
            vars.Add ("SearchResultForUserText", translator.GetTranslatedString ("SearchResultForUserText"));
            vars.Add ("EditText", translator.GetTranslatedString ("EditText"));
            vars.Add ("EditUserAccountText", translator.GetTranslatedString ("EditUserAccountText"));

            return vars;
        }

        public bool AttemptFindPage (string filename, ref OSHttpResponse httpResponse, out string text)
        {
            text = "";
            return false;
        }
    }
}

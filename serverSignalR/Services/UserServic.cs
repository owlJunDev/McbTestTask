using System.Collections.Generic;

namespace Server.Services
{
    public class UsrService
    {
        private static readonly Dictionary<String, String> Users = new Dictionary<String, String>();
        public bool AddUserToList(String userToAdd, String idToAdd)
        {
            lock (Users)
            {
                foreach (var user in Users)
                    if (user.Key.ToLower() == userToAdd.ToLower())
                        return false;

                Users.Add(userToAdd, idToAdd);
                return true;
            }
        }
        public String GetUserByConnectionId(String connectionId)
        {
            lock (Users)
            {
                return Users.Where(x => x.Value == connectionId).Select(x => x.Key).FirstOrDefault();
            }
        }
        public void RemoveUserFromList(String user)
        {
            lock (Users)
            {
                if (Users.ContainsKey(user))
                    Users.Remove(user);
            }
        }
        public String[] GetOnlineUsersId() {
            lock (Users) {
                return Users.OrderBy(x => x.Key).Select(x => x.Value).ToArray();
            }
        }
        public int GetOnlineCount() {
            return Users.Count;
        }
    }
}
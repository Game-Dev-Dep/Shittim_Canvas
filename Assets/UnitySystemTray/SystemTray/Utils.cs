using System;
using System.Collections.Generic;

namespace Utils
{
    public static partial class TrayIcon
    {
        private static ushort _id = 0;

        private static ushort GetUniqueID()
        {
            if (_id == 0)
            {
                long ticks = DateTime.UtcNow.Ticks;
                _id = (ushort)(ticks % ushort.MaxValue);
            }
            return ++_id;
        }

        private static void ProcessMenuActions(List<(string, Action)> actions)
        {
            MenuActions = new Dictionary<string, Action>();
            ActionMappings = new Dictionary<uint, string>();
            SubMenus = new Dictionary<string, List<(string, Action)>>();
            SubMenuMappings = new Dictionary<uint, string>();

            if (actions == null)
                return;

            foreach (var (label, callback) in actions)
            {
                if (label == LEFT_CLICK)
                {
                    OnLeftClick = callback;
                    continue;
                }

                uint uid = GetUniqueID();
                ActionMappings[uid] = label;
                MenuActions[label] = callback;
            }
        }

        /// <summary>
        /// 创建子菜单
        /// </summary>
        /// <param name="parentMenuLabel">父菜单标签</param>
        /// <param name="subMenuItems">子菜单项列表</param>
        public static void CreateSubMenu(string parentMenuLabel, List<(string, Action)> subMenuItems)
        {
            if (SubMenus == null)
                SubMenus = new Dictionary<string, List<(string, Action)>>();
            
            SubMenus[parentMenuLabel] = subMenuItems;
        }

        private static string TruncateString(string str, int maxLength)
        {
            if (string.IsNullOrEmpty(str)) return "";
            return str.Length < maxLength ? str : str.Substring(0, maxLength - 1);
        }
    }
}

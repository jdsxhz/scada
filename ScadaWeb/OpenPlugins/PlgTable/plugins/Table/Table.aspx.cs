﻿/*
 * Copyright 2016 Mikhail Shiryaev
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 * 
 * 
 * Product  : Rapid SCADA
 * Module   : PlgSchemeCommon
 * Summary  : Table view web form
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2016
 * Modified : 2016
 */

using Scada.Data;
using Scada.UI;
using Scada.Web.Shell;
using System;
using System.Text;
using System.Web.UI.WebControls;

namespace Scada.Web.Plugins.Table
{
    /// <summary>
    /// Table view web form
    /// <para>Веб-форма табличного представления</para>
    /// </summary>
    public partial class WFrmTable : System.Web.UI.Page
    {
        /// <summary>
        /// Относительный путь к иконкам величин
        /// </summary>
        private const string QuantityIconsPath = "images/quantityicons/";
        /// <summary>
        /// Иконка величины по умолчанию
        /// </summary>
        private const string DefQuantityIcon = "quantity.png";

        // Переменные для вывода на веб-страницу
        protected bool debugMode = false; // режим отладки
        protected int viewID;             // ид. представления
        protected int refrRate;           // частота обновления данных
        protected string phrases;         // локализованные фразы
        protected string tableViewHtml;   // HTML-код табличного представления


        /// <summary>
        ///Получить час в соответствии с текущей культурой
        /// </summary>
        private string GetLocalizedHour(int hour)
        {
            return DateTime.MinValue.AddHours(hour).ToString("t", Localization.Culture);
        }

        /// <summary>
        /// Заполнить выпадающие списки выбора времени
        /// </summary>
        private void FillTimeDropdowns()
        {
            for (int hour = 0; hour < 24; hour++)
            {
                string text = GetLocalizedHour(hour);
                string value = hour.ToString();
                ddlTimeFrom.Items.Add(new ListItem(text, value));
                ddlTimeTo.Items.Add(new ListItem(text, value));
            }

            for (int hour = 0, val = -24; hour < 24; hour++, val++)
            {
                string text = PlgPhrases.PrevDay + " " + GetLocalizedHour(hour);
                ddlTimeFrom.Items.Add(new ListItem(text, val.ToString()));
            }
        }

        /// <summary>
        /// Добавить ячейку заголовка
        /// </summary>
        private void AppendHeaderCell(StringBuilder sbHtml, string cssClass, string innerHtml)
        {
            if (string.IsNullOrEmpty(cssClass))
                sbHtml.Append("<th>");
            else
                sbHtml.Append("<th class='").Append(cssClass).Append("'>");

            sbHtml.Append(innerHtml).Append("</th>");
        }

        /// <summary>
        /// Добавить ячейку
        /// </summary>
        private void AppendCell(StringBuilder sbHtml, string cssClass, int? hour, string innerHtml)
        {
            sbHtml.Append("<td");

            if (!string.IsNullOrEmpty(cssClass))
                sbHtml.Append(" class='").Append(cssClass).Append("'");

            if (hour.HasValue)
                sbHtml.Append(" data-hour='").Append(hour.Value).Append("'");

            sbHtml.Append(">").Append(innerHtml).Append("</td>");
        }

        /// <summary>
        /// Генерировать HTML-код табличного представления
        /// </summary>
        private string GenerateTableViewHtml(TableView tableView, bool cmdEnabled)
        {
            StringBuilder sbHtml = new StringBuilder();
            sbHtml.AppendLine("<table>");

            // заголовок таблицы
            sbHtml.AppendLine("<tr class='hdr'>");
            AppendHeaderCell(sbHtml, "", "Item");
            AppendHeaderCell(sbHtml, "cur", "Current");
            for (int hour = 0; hour < 24; hour++)
                AppendHeaderCell(sbHtml, "", GetLocalizedHour(hour));
            sbHtml.AppendLine().AppendLine("</tr>");

            // строки таблицы
            bool altRow = false;
            foreach(TableView.Item item in tableView.VisibleItems)
            {
                InCnlProps cnlProps = item.CnlProps;
                int cnlNum = item.CnlNum;
                int ctrlCnlNum = cmdEnabled ? item.CtrlCnlNum : 0;

                // тег начала строки
                sbHtml.AppendLine(altRow ? "<tr class='item alt'" : "<tr class='item'");
                if (cnlNum > 0)
                    sbHtml.Append(" data-cnl='").Append(cnlNum).Append("'");
                if (ctrlCnlNum > 0)
                    sbHtml.Append(" data-ctrl='").Append(ctrlCnlNum).Append("'");
                sbHtml.AppendLine(">");

                // ячейка наименования
                string caption = string.IsNullOrEmpty(item.Caption) ? "&nbsp;" : item.Caption;
                if (cnlNum > 0 || ctrlCnlNum > 0)
                {
                    StringBuilder sbCapHtml = new StringBuilder();
                    string iconFileName = cnlProps == null || cnlProps.IconFileName == "" ?
                        DefQuantityIcon : cnlProps.IconFileName;
                    sbCapHtml.Append("<img src='" + QuantityIconsPath + iconFileName + "' alt='' />")
                        .Append("<a href='#'>").Append(caption).Append("</a>");
                    if (ctrlCnlNum > 0)
                        sbCapHtml.Append("<span class='cmd' title='Send Command'></span>");
                    AppendCell(sbHtml, "cap", null, sbCapHtml.ToString());
                }
                else
                {
                    AppendCell(sbHtml, "cap", null, caption);
                }

                // ячейки текущих и часовых данных
                if (cnlNum > 0)
                {
                    AppendCell(sbHtml, "cur", null, "---");
                    for (int hour = 0; hour < 24; hour++)
                        AppendCell(sbHtml, "hour", hour, "---");
                }
                else
                {
                    AppendCell(sbHtml, "cur", null, "");
                    for (int hour = 0; hour < 24; hour++)
                        AppendCell(sbHtml, "hour", hour, "");
                }

                // тег окончания строки
                sbHtml.AppendLine().AppendLine("</tr>");
                altRow = !altRow;
            }

            sbHtml.AppendLine("</table>");
            return sbHtml.ToString();
        }


        protected void Page_Load(object sender, EventArgs e)
        {
            AppData appData = AppData.GetAppData();
            UserData userData = UserData.GetUserData();

#if DEBUG
            debugMode = true;
            userData.LoginForDebug();
#endif

            // перевод веб-страницы
            Translator.TranslatePage(Page, "Scada.Web.Plugins.Table.WFrmTable");

            // получение ид. представления из параметров запроса
            int.TryParse(Request.QueryString["viewID"], out viewID);

            // проверка прав на просмотр представления
            EntityRights rights = userData.LoggedOn ? 
                userData.UserRights.GetViewRights(viewID) : EntityRights.NoRights;
            if (!rights.ViewRight)
                Response.Redirect(UrlTemplates.NoView);

            // загрузка представления в кеш,
            // ошибка будет записана в журнал приложения
            TableView tableView;
            try
            {
                tableView = appData.ViewCache.GetView<TableView>(viewID, true);
                appData.AssignStamp(tableView);
            }
            catch
            {
                tableView = null;
                Response.Redirect(UrlTemplates.NoView);
            }

            // подготовка данных для вывода на веб-страницу
            refrRate = userData.WebSettings.DataRefrRate;

            Localization.Dict dict;
            Localization.Dictionaries.TryGetValue("Scada.Web.Plugins.Table.WFrmTable.Js", out dict);
            phrases = WebUtils.DictionaryToJs(dict);

            bool cmdEnabled = userData.WebSettings.CmdEnabled && rights.ControlRight;
            tableViewHtml = GenerateTableViewHtml(tableView, cmdEnabled);

            FillTimeDropdowns();
        }
    }
}
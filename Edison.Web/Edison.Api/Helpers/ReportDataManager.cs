﻿using Edison.Core.Common.Models;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;
using Edison.Common.Interfaces;
using Edison.Common.DAO;
using System.IO;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.Drawing;
using Edison.Api.Config;
using NPOI.SS.Util;

namespace Edison.Api.Helpers
{
    public class ReportDataManager
    {
        private readonly WebApiOptions _config;
        private readonly ICosmosDBRepository<ResponseDAO> _repoResponses;
        private readonly ICosmosDBRepository<EventClusterDAO> _repoEventClusters;
        private readonly ICosmosDBRepository<ChatReportDAO> _repoChatReports;
        private readonly Dictionary<string, int> _mappingUserColumns;
        private readonly Dictionary<string, int> _mappingConversationColumns;
        private readonly Dictionary<string, int> _mappingEventColumns;

        public ReportDataManager(IOptions<WebApiOptions> config,
            ICosmosDBRepository<ResponseDAO> repoResponses, 
            ICosmosDBRepository<EventClusterDAO> repoEventClusters,
            ICosmosDBRepository<ChatReportDAO> repoChatReports)
        {
            _config = config.Value;
            _repoResponses = repoResponses;
            _repoEventClusters = repoEventClusters;
            _repoChatReports = repoChatReports;

            _mappingConversationColumns = new Dictionary<string, int>();
            _mappingUserColumns = new Dictionary<string, int>();
            _mappingEventColumns = new Dictionary<string, int>();
            SetupMappingTable();
        }
        
        public async Task<byte[]> GetReport(ReportCreationModel reportRequest)
        {
            reportRequest.MinimumDate = DateTime.UtcNow.AddDays(-30);
            reportRequest.MaximumDate = DateTime.UtcNow.AddDays(30);
            reportRequest.Type = ReportCreationType.Responses | ReportCreationType.Events | ReportCreationType.Conversations;

            byte[] export = null;

            using (MemoryStream stream = new MemoryStream())
            {
                IWorkbook workbook = new XSSFWorkbook();

                if ((reportRequest.Type & ReportCreationType.Events) != 0)
                    await GenerateEventsReport(workbook, reportRequest.MinimumDate, reportRequest.MaximumDate);

                if ((reportRequest.Type & ReportCreationType.Conversations) != 0)
                {
                    //Handle Conversations
                }

                //Handle Responses
                if ((reportRequest.Type & ReportCreationType.Responses) != 0)
                    await GenerateResponsesReport(workbook, reportRequest.MinimumDate, reportRequest.MaximumDate);

                workbook.Write(stream);
                export = stream.ToArray();
            }

            return export;
        }

        private async Task GenerateEventsReport(IWorkbook workbook, DateTime? requestedMinDate, DateTime? requestedMaxDate)
        {
            //Retrieve responses
            var eventClusters = await GetListBetweenDates(_repoEventClusters, requestedMinDate, requestedMaxDate);
            if (eventClusters != null)
            {
                ISheet eventClustersEventSheet = workbook.CreateSheet($"Events");
                GenerateEventsReport(eventClustersEventSheet, 0, eventClusters);
            }
        }

        private async Task GenerateResponsesReport(IWorkbook workbook, DateTime? requestedMinDate, DateTime? requestedMaxDate)
        {
            //Retrieve responses
            var responses = await GetListBetweenDates(_repoResponses, requestedMinDate, requestedMaxDate);
            if (responses != null)
            {
                //Figuring out the latest enddate. If one enddate = null, then latest endate = current date
                DateTime maxDate = DateTime.UtcNow;
                if (responses.Where(p => p.EndDate.Value == null) == null)
                    maxDate = responses.Max(p => p.EndDate).Value;

                //Retrieve all associated event clusters and chat reports
                var eventClusters = await GetListBetweenDates(_repoEventClusters, requestedMinDate, maxDate);
                var chatReports = await GetListBetweenDates(_repoChatReports, requestedMinDate, maxDate);

                //Enum responses
                foreach (var response in responses)
                {
                    ISheet responseEventSheet = workbook.CreateSheet($"{response.ActionPlan.Name} - Events - {response.Id}");
                    IEnumerable<EventClusterDAO> responseEventClusters = eventClusters.Where(e => response.EventClusterIds.Any(r => r.ToString() == e.Id));
                    GenerateResponseHeaderReport(workbook, responseEventSheet, 0, response);
                    GenerateEventsReport(responseEventSheet, responseEventSheet.LastRowNum + 1, responseEventClusters);
                }

            }
        }

        private void GenerateResponseHeaderReport(IWorkbook workbook, ISheet sheet, int rowStartIndex, ResponseDAO response)
        {
            List<ReportResponseHeaderRowOptions> responseHeaders = _config.ReportConfiguration.ResponseHeader;
            if (responseHeaders == null)
                return;

            foreach (var responseHeaderRow in responseHeaders)
            {
                IRow row = sheet.CreateRow(rowStartIndex + responseHeaderRow.RowIndex);
                foreach (var responseHeaderColumn in responseHeaderRow.Columns)
                {
                    var value = GetResponseHeaderValue(responseHeaderColumn.Value, response);
                    if(value is double)
                        SetCellValue(row, responseHeaderColumn.ColumnIndex, value, GetStyle(workbook, responseHeaderColumn.Style), ReportDataType.Double);
                    else
                        SetCellValue(row, responseHeaderColumn.ColumnIndex, value, GetStyle(workbook, responseHeaderColumn.Style), ReportDataType.Text);
                }

            }
        }

        private void GenerateEventsReport(ISheet sheet, int rowStartIndex, IEnumerable<EventClusterDAO> eventClusters)
        {
            Dictionary<string, XSSFCellStyle> rowCellStyles = SetupRowStyles(sheet.Workbook, _config.ReportConfiguration.EventsReport);
            SetupColumns(sheet, rowStartIndex, _config.ReportConfiguration.EventsReport);
            int rowIndex = rowStartIndex + 1;

            //Enum event clusters
            foreach (var eventCluster in eventClusters)
            {
                //Enum events
                foreach (var eventObj in eventCluster.Events)
                {
                    IRow row = sheet.CreateRow(rowIndex);

                    SetCellValue(row, _mappingEventColumns["eventClusterId"], eventCluster.Id.ToString(), rowCellStyles["eventClusterId"]);
                    SetCellValue(row, _mappingEventColumns["deviceId"], eventCluster.Device?.DeviceId.ToString(), rowCellStyles["deviceId"]);
                    SetCellValue(row, _mappingEventColumns["eventDate"], eventObj.Date.ToOADate(), rowCellStyles["eventDate"], ReportDataType.Date);
                    SetCellValue(row, _mappingEventColumns["eventType"], eventCluster.EventType, rowCellStyles["eventType"]);
                    SetCellValue(row, _mappingEventColumns["deviceType"], eventCluster.Device?.DeviceType, rowCellStyles["deviceType"]);
                    SetCellValue(row, _mappingEventColumns["deviceName"], eventCluster.Device?.Name, rowCellStyles["deviceName"]);
                    SetCellValue(row, _mappingEventColumns["deviceLocation1"], eventCluster.Device?.Location1, rowCellStyles["deviceLocation1"]);
                    SetCellValue(row, _mappingEventColumns["deviceLocation2"], eventCluster.Device?.Location2, rowCellStyles["deviceLocation2"]);
                    SetCellValue(row, _mappingEventColumns["deviceLocation3"], eventCluster.Device?.Location3, rowCellStyles["deviceLocation3"]);
                    SetCellValue(row, _mappingEventColumns["deviceGeolocationLon"], eventCluster.Device?.Geolocation?.Longitude, rowCellStyles["deviceGeolocationLon"], ReportDataType.Double);
                    SetCellValue(row, _mappingEventColumns["deviceGeolocationLat"], eventCluster.Device?.Geolocation?.Latitude, rowCellStyles["deviceGeolocationLat"], ReportDataType.Double);
                    SetCellValue(row, _mappingEventColumns["eventMetadata"], string.Join(", ", eventObj.Metadata.Select(x => x.Key + ": " + x.Value)), rowCellStyles["eventMetadata"]);

                    rowIndex++;
                }
            }
        }

        #region Utility Methods
        private void SetCellValue(IRow row, int cellIndex, object value, XSSFCellStyle cellStyle, ReportDataType dataType = ReportDataType.Text)
        {
            ICell cell = row.CreateCell(cellIndex);

            switch (dataType)
            {
                case ReportDataType.Date:
                case ReportDataType.Double:
                    cell.SetCellType(CellType.Numeric);
                    cell.SetCellValue((double)value);
                    break;
                case ReportDataType.Integer:
                    cell.SetCellType(CellType.Numeric);
                    cell.SetCellValue((int)value);
                    break;
                case ReportDataType.RichText:
                    cell.SetCellType(CellType.String);
                    cell.SetCellValue((IRichTextString)value);
                    break;
                default:
                    cell.SetCellType(CellType.String);
                    cell.SetCellValue((string)value);
                    break;
            }
            cell.CellStyle = cellStyle;
        }

        private void SetupColumns(ISheet sheet, int rowIndex, IEnumerable<ReportColumnOptions> columnsOptions)
        {
            IWorkbook workbook = sheet.Workbook;
            IRow row = sheet.CreateRow(rowIndex);

            foreach (var columnOptions in columnsOptions.OrderBy(p => p.ColumnIndex))
            {
                //Set up column style
                XSSFCellStyle cellStyle = GetStyle(workbook, columnOptions.HeaderStyle);
                //Set Hidden state
                sheet.SetColumnHidden(columnOptions.ColumnIndex, columnOptions.IsHidden);
                //Set Column Width
                sheet.SetColumnWidth(columnOptions.ColumnIndex, GetColumnWidth(columnOptions.Width));

                //Create Cell
                ICell cell = row.CreateCell(columnOptions.ColumnIndex);
                cell.SetCellValue(columnOptions.HeaderName);
                cell.CellStyle = cellStyle;
            }

            //Add freezing Pane + Filtering
            sheet.CreateFreezePane(0, rowIndex + 1);
            sheet.SetAutoFilter(new CellRangeAddress(rowIndex, rowIndex, 0, columnsOptions.Count() - 1));
        }

        private XSSFCellStyle GetStyle(IWorkbook workbook, ReportStyleCell reportStyle)
        {
            ICreationHelper creationHelper = workbook.GetCreationHelper();

            //Set up column style
            XSSFCellStyle cellStyle = (XSSFCellStyle)workbook.CreateCellStyle();
            if (reportStyle == null)
                return cellStyle;

            XSSFFont font = (XSSFFont)workbook.CreateFont();
            font.Boldweight = (short)reportStyle.FontWeight;

            //DataFormat
            if (!string.IsNullOrEmpty(reportStyle.DataFormat))
            {
                cellStyle.SetDataFormat(creationHelper.CreateDataFormat().GetFormat(reportStyle.DataFormat));
                cellStyle.Alignment = reportStyle.HorizontalAlignment;
            }

            //BackgroundColor
            if (!string.IsNullOrEmpty(reportStyle.BackgroundColor))
            {
                XSSFColor backgroundColor = new XSSFColor(Color.FromName(reportStyle.BackgroundColor));
                cellStyle.FillPattern = FillPattern.SolidForeground;
                cellStyle.SetFillForegroundColor(backgroundColor);
            }
            //Foreground Color
            if (!string.IsNullOrEmpty(reportStyle.FontColor))
            {
                XSSFColor foregroundColor = new XSSFColor(Color.FromName(reportStyle.FontColor));
                font.SetColor(foregroundColor);
            }
            //Wrap Text
            cellStyle.SetFont(font);
            cellStyle.WrapText = reportStyle.WrapText;

            return cellStyle;
        }

        private void SetupMappingTable()
        {
            _mappingConversationColumns.Clear();
            _mappingUserColumns.Clear();
            _mappingEventColumns.Clear();

            if (_config.ReportConfiguration != null)
            {
                if (_config.ReportConfiguration.ConversationsReport != null)
                    foreach (var conversationColumn in _config.ReportConfiguration.ConversationsReport)
                        _mappingConversationColumns.Add(conversationColumn.Name, conversationColumn.ColumnIndex);

                if (_config.ReportConfiguration.UsersReport != null)
                    foreach (var userColumn in _config.ReportConfiguration.UsersReport)
                        _mappingUserColumns.Add(userColumn.Name, userColumn.ColumnIndex);

                if (_config.ReportConfiguration.EventsReport != null)
                    foreach (var eventColumn in _config.ReportConfiguration.EventsReport)
                        _mappingEventColumns.Add(eventColumn.Name, eventColumn.ColumnIndex);
            }
        }

        private Dictionary<string, XSSFCellStyle> SetupRowStyles(IWorkbook workbook, IEnumerable<ReportColumnOptions> columnsOptions)
        {
            if (columnsOptions == null)
                return new Dictionary<string, XSSFCellStyle>();

            Dictionary<string, XSSFCellStyle> output = new Dictionary<string, XSSFCellStyle>();
            foreach (var columnOptions in columnsOptions)
            {
                //Get style
                XSSFCellStyle cellStyle = GetStyle(workbook, columnOptions.RowStyle);
                //Output
                output.Add(columnOptions.Name, cellStyle);
            }

            return output;
        }

        private async Task<IEnumerable<T>> GetListBetweenDates<T>(ICosmosDBRepository<T> repository, DateTime? minDate, DateTime? maxDate) where T : IEntityDAO
        {
            var results = await repository.GetItemsAsync(p =>
            (minDate == null || p.CreationDate >= minDate.Value) &&
            (maxDate == null || p.CreationDate <= maxDate.Value));
            return results;
        }

        private int GetColumnWidth(int width)
        {
            if (width > 254)
                return 65280; // Maximum allowed column width. 
            if (width > 1)
            {
                int floor = (int)(Math.Floor(((double)width) / 5));
                int factor = (30 * floor);
                int value = 450 + factor + ((width - 1) * 250);
                return value;
            }
            else
                return 450; // default to column size 1 if zero, one or negative number is passed. 
        }

        private object GetResponseHeaderValue(string key, ResponseDAO response)
        {
            switch (key)
            {
                case "{RESPONSETYPE}":
                    return response.ActionPlan.Name;
                case "{RESPONSEDESCRIPTION}":
                    return response.ActionPlan.Description;
                case "{PRIMARYRADIUS}":
                    return response.ActionPlan.PrimaryRadius.ToString();
                case "{SECONDARYRADIUS}":
                    return response.ActionPlan.SecondaryRadius.ToString();
                case "{PRIMARYEVENTCLUSTERID}":
                    return response.PrimaryEventClusterId.ToString();
                case "{LONGITUDE}":
                    return response.Geolocation?.Longitude.ToString();
                case "{LATITUDE}":
                    return response.Geolocation?.Latitude.ToString();
                case "{CREATIONDATE}":
                    return response.CreationDate.ToOADate();
                case "{ENDDATE}":
                    if (response.EndDate == null)
                        return "ONGOING";
                    return response.EndDate.Value.ToOADate();
            }
            return key;
        }
        #endregion
    }
}

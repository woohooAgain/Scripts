using System;
using System.Linq;
using System.Collections.Generic;
using Comindware.Data.Entity;

class Script
{
    public static void Main(Comindware.Process.Api.Data.ScriptContext context, Comindware.Entities entities)
    {
        var sessioinNumeratorListAlias = "sessionNumeratorList";
        var sessionsForTomorrowListName = "numeratorList";

        var session_start_dateProperty = "op.12";
        var session_training_apparatusProperty = "op.7";
        var calculatedCompanyIdProperty = "op.1010";
        var sessionNumberProperty = "op.220";

        var sessionStatusProperty = "op.116";

        var sessionRTId = "oa.5";
        var sessionsForTomorrowRTId = "oa.43";

        var S7InvetsId = "72848";

        var systemCompanies = new List<string>();
        systemCompanies.Add("473");
        systemCompanies.Add("22967");
        systemCompanies.Add("5481");
        systemCompanies.Add("23488");

        var businessObject = Api.TeamNetwork.ObjectService.Get(context.BusinessObjectId);
        object startPeriodObject;
        object endPeriodObject;
        
        businessObject.TryGetValue("op.663", out startPeriodObject);
        businessObject.TryGetValue("op.662", out endPeriodObject);

        var startPeriod = (DateTime)startPeriodObject;
        var endPeriod = (DateTime)endPeriodObject;

        string sessionNumeratorDatasetConfigId;
        var sessionLists = Api.TeamNetwork.DatasetConfigurationService.List(sessionRTId);
        if (sessionLists.Select(l => l.Name).Contains(sessioinNumeratorListAlias))
        {
            var sessionDatasetId = sessionLists.FirstOrDefault(l => l.Name == sessioinNumeratorListAlias).Id;
            sessionNumeratorDatasetConfigId = Api.TeamNetwork.DatasetConfigurationService.Get(sessionDatasetId)?.Id;
        }
        else
        {
            var columns = new List<Comindware.TeamNetwork.Api.Data.ColumnConfiguration>
            {
                new Comindware.TeamNetwork.Api.Data.ColumnConfiguration
                {
                    DataSourceInfo = new Comindware.TeamNetwork.Api.Data.DataSourceInfo
                    {
                        PropertyPath = new[] { session_start_dateProperty }
                    }
                },
                new Comindware.TeamNetwork.Api.Data.ColumnConfiguration
                {
                    DataSourceInfo = new Comindware.TeamNetwork.Api.Data.DataSourceInfo
                    {
                        PropertyPath = new[] { session_training_apparatusProperty }
                    }
                },
                new Comindware.TeamNetwork.Api.Data.ColumnConfiguration
                {
                    DataSourceInfo = new Comindware.TeamNetwork.Api.Data.DataSourceInfo
                    {
                        PropertyPath = new[] { calculatedCompanyIdProperty }
                    }
                },
                new Comindware.TeamNetwork.Api.Data.ColumnConfiguration
                {
                    DataSourceInfo = new Comindware.TeamNetwork.Api.Data.DataSourceInfo
                    {
                        PropertyPath = new[] { sessionStatusProperty }
                    }
                },
                new Comindware.TeamNetwork.Api.Data.ColumnConfiguration
                {
                    DataSourceInfo = new Comindware.TeamNetwork.Api.Data.DataSourceInfo
                    {
                        PropertyPath = new[] { "isDisabled" }
                    }
                }
            };

            sessionNumeratorDatasetConfigId = Api.TeamNetwork.DatasetConfigurationService.Create(new Comindware.TeamNetwork.Api.Data.DatasetConfiguration
            {
                Name = sessioinNumeratorListAlias,
                ContainerId = sessionRTId,
                Columns = columns,
                Paging = new Comindware.TeamNetwork.Api.Data.PagingConfiguration
                {
                    Size = 1000000000
                },
                SystemFilterExpression = @"@prefix xsd: <http://www.w3.org/2001/XMLSchema#>.
                    @prefix cmw: <http://comindware.com/logics#>.
                    @prefix axis: <http://comindware.com/logics/axis#>.
                    @prefix log: <http://www.w3.org/2000/10/swap/log#>.
                    @prefix object: <http://comindware.com/ontology/object#>.
                    @prefix user: <http://comindware.com/ontology/user#>.
                    @prefix property: <http://comindware.com/ontology/user/op#>.
                    @prefix session: <http://comindware.com/ontology/session#>.
                    @prefix time: <http://comindware.com/logics/time#>.
                    @prefix math: <http://www.w3.org/2000/10/swap/math#>.
                    @prefix cmwtime: <http://comindware.com/logics/time#>.

                    {
                        (""session_statys""^^xsd:string ""session_statys_name""^^xsd:string) object:findProperty ?propertystat.
                        (""sessions""^^xsd:string ""session_statys""^^xsd:string) object:findProperty ?propertysesstat.
                        (""sessions""^^xsd:string ""session_start_date""^^xsd:string) object:findProperty ?propertyStartDate.
                        (""sessions""^^xsd:string ""calculatedCompanyId""^^xsd:string) object:findProperty ?propertycalculatedCompanyId.

                        # +(0h-3h) in seconds
                        ""-10800""^^xsd:double <http://comindware.com/logics/time#toDuration> ?startDayOffset.
                        # +(24h-3h) in seconds
                        ""75600""^^xsd:double <http://comindware.com/logics/time#toDuration> ?endDayOffset.

                        session:context session:requestTime ?nowUTC.
                        ?nowUTC <http://comindware.com/ontology/entity/nullable#startOfDay> ?startOfCurrentDayUTC.
                        (?startOfCurrentDayUTC ?startDayOffset) <http://comindware.com/logics/time#addDuration> ?startTime.
                        (?startOfCurrentDayUTC ?endDayOffset) <http://comindware.com/logics/time#addDuration> ?endTime.

                        ?statD ?propertystat ""Удалена"".
                        ?statR ?propertystat ""Отменена (неявка)"".
                        ?container object:alias ""sessions"".
                        ?item cmw:container ?container.
                        not{?item ?propertysesstat ?statD.}.
                        not{?item ?propertysesstat ?statR.}.

                        ?item ?propertyStartDate ?startDate.
                        ?startDate math:notLessThan ?startTime.
                        ?startDate math:lessThan ?endTime.
                    }"
            });
        }

        var query = new Comindware.TeamNetwork.Api.Data.DatasetQuery
        {
            DatasetId = sessionNumeratorDatasetConfigId
        };

        var sessionsData = Api.TeamNetwork.DatasetService.QueryData(query).Rows;
        var sessionList = new List<List<SessionModel>>
        {
            new List<SessionModel>(),
            new List<SessionModel>(),
            new List<SessionModel>(),
            new List<SessionModel>(),
            new List<SessionModel>(),
        };
        foreach (var session in sessionsData)
        {            
            switch ((session.Data[1] as Comindware.TeamNetwork.Api.Data.Reference)?.Id)
            {
                case("87312"):
                    //landingAreaSimulator
                    FillSimulator(sessionList[0], session);            
                    break;
                case("31976"):
                    //mftdSimulator
                    FillSimulator(sessionList[1], session);
                    break;
                case("64"):
                    //a3201Simulator
                    FillSimulator(sessionList[2], session);
                    break;
                case("63"):
                    //a3202Simulator 
                    FillSimulator(sessionList[3], session);
                    break;
                case("62"):
                    //b737Simulator
                    FillSimulator(sessionList[4], session);   
                    break;
            }
        }

        //isSystem ? 11 : 1;
        foreach (var list in sessionList)
        {
            foreach (var session in list)
            {                        
                int number;
                if (session.CompanyId == S7InvetsId)
                {
                    number = 0;
                }
                else if (systemCompanies.Contains(session.CompanyId))
                {                    
                    number = list.Count(s => systemCompanies.Contains(s.CompanyId) && s.StartDate < session.StartDate) + 11;
                }              		 
                else 
                {
                    number = list.Count(s => !systemCompanies.Contains(s.CompanyId) && s.CompanyId != S7InvetsId && s.StartDate < session.StartDate) + 1;                    
                } 
                Api.TeamNetwork.ObjectService.Edit(session.Id, new Dictionary<string, object>{{sessionNumberProperty, number}});  
            }
        } 
    }

    private static void FillSimulator(List<SessionModel> simulator, Comindware.TeamNetwork.Api.Data.RowData sessionData)
    {
        simulator.Add( new SessionModel
        {
            Id = sessionData.Id.ToString(),
            StartDate = (DateTime)sessionData.Data[0],
            CompanyId = sessionData.Data[2] as string
        });
    }

    public class SessionModel
    {
        public string CompanyId { get; set; }
        public string Id { get; set; }
        public string SimulatorId { get; set; }
        public DateTime StartDate { get; set; }
        public string Status { get; set; }
        public int Number { get; set; }
    }
}
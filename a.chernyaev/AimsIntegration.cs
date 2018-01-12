using System;
using System.Collections.Generic;
using System.Linq;
using Comindware.Data.Entity;
using System.IO;
using System.Globalization;
using System.Text;

class Script
{
    public static void Main(Comindware.Process.Api.Data.ScriptContext context, Comindware.Entities entities)
    {
        string path = @"C:\report\CTPcopy.csv";
        string pathTest = @"C:\report\testAims.txt";
        string text;
        using (StreamReader sr = new System.IO.StreamReader(path))
        {
                text = sr.ReadToEnd();
        }
        List<Session> sessionFile = new List<Session>();

        DateTime startDate = DateTime.MaxValue;
        DateTime finishDate = DateTime.MinValue;

        using (var sw = new System.IO.StreamWriter(pathTest))
        {
            foreach (var line in text.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var column = line.Split(';');
                if (column.Length < 13)
                {
                    sw.WriteLine("column.Length < 13");
                    continue;
                }
                DateTime current_session_start_date;

                sw.WriteLine(column[0]);
                sw.WriteLine(column[0].Trim());
                //DateTime.TryParse(column[0].Trim(), out current_session_start_date);

                //var provider = CultureInfo.InvariantCulture;
                var provider = new CultureInfo("ru-RU");
                //05.12.2017 23:45
                var format = "dd.MM.yyyy HH:mm:ss";

                current_session_start_date = DateTime.ParseExact(column[0], format, provider);
                //sw.WriteLine(result);

                sw.WriteLine(current_session_start_date);

                current_session_start_date = TimeZoneInfo.ConvertTime(current_session_start_date, TimeZoneInfo.Utc, TimeZoneInfo.Local);

                sw.WriteLine(current_session_start_date);

                

                DateTime current_session_finish_date;
                //DateTime.TryParse(column[1].Trim(), out current_session_finish_date);
                current_session_finish_date = DateTime.ParseExact(column[1].Trim(), format, provider);
                current_session_finish_date = TimeZoneInfo.ConvertTime(current_session_finish_date, TimeZoneInfo.Utc, TimeZoneInfo.Local);

                sw.WriteLine(current_session_finish_date);

                sessionFile.Add(new Session()
                {
                    session_start_date = current_session_start_date,
                    session_finish_date = current_session_finish_date,
                    session_training_apparatus_str = column[2].Trim(),
                    typeAIMS_str = column[3].Trim(),//secondary parameter
                    fbs_str = column[4].Trim(),//secondary parameter
                    leaseWet_str = column[5].Trim(),
                    session_company_str = column[6].Trim(),
                    instructor_str = column[7].Trim(),//secondary parameter
                    pilot1_str = column[8].Trim(),//secondary parameter
                    pilot2_str = column[9].Trim(),//secondary parameter
                    code_str = column[10].Trim(),//secondary parameter
                    session_codeAIMS_str = column[11].Trim(),//secondary parameter
                    observer_str = column[12].Trim()
                });

                if (current_session_start_date < startDate.ToUniversalTime())
                {
                    startDate = current_session_start_date.ToUniversalTime();
                }
                if (current_session_finish_date > finishDate.ToUniversalTime())
                {
                    finishDate = current_session_finish_date.ToUniversalTime();
                }
            }

            //Выбор всех сессий, с которыми потенциально можно пересечься при создании новых сессий
            var allSessions =
                entities.sessions.Where(s => s.session_finish_date.HasValue && s.session_start_date.HasValue && (startDate < s.session_finish_date.Value) && (s.session_start_date.Value < finishDate)
                && s._isDisabled != true && !s.session_name.Contains("ОЭТ (15)") && s.ticket != null)
                    .Select(s => new
                    {
                        s.id,
                        s.session_start_date,
                        s.session_finish_date,
                        s.session_training_apparatus,
                        s.ticket,
                        s.typeAIMS,
                        s.fbs,
                        s.instructor,
                        s.first_pilot,
                        s.second_pilot,
                        s.codeAIMS
                    })
                    .ToList();

            //Выбор всех тренажёров                
            var trainingApparatusDictionary = entities.training_apparatus.Where(x => x.codeAIMS != null).ToDictionary(x => x.codeAIMS, x => x.id);

            var ticketDictionary = new Dictionary<string, string>();
            var ticketList = entities.tickets.Where(t => (t.company_ticket != null)).Select(t => new { t.id, t.company_ticket }).ToList();
            foreach (var ticket in ticketList)
            {
                if (String.IsNullOrEmpty(ticket.company_ticket) || ticketDictionary.ContainsKey(ticket.id))
                {
                    continue;
                }
                else
                {
                    ticketDictionary.Add(ticket.id, ticket.company_ticket);
                }
            }



            foreach (var line in sessionFile)
            {
                //Определение заявки
                string session_ticket = null;
                session_ticket = entities.company.Where(c => c.codeAIMS == line.session_company_str && c._isDisabled != true)
                    .Select(t => t.ticket_aims)
                    .FirstOrDefault();
                if (String.IsNullOrEmpty(session_ticket))
                {
                    continue;
                }

                //Определение тренажёра
                string training_apparatus = null;
                if (line.session_training_apparatus_str != null)
                {
                    trainingApparatusDictionary.TryGetValue(line.session_training_apparatus_str, out training_apparatus);
                }

                bool fbs = false;
                if (line.fbs_str == "FBS")
                {
                    fbs = true;
                }

                bool leaseWET = !String.IsNullOrWhiteSpace(line.leaseWet_str);

                string sessionStatus = "423";// Статус "Забронирована"

                //Определение инструктора
                string instructor = null;
                if (!String.IsNullOrEmpty(line.instructor_str))
                {
                    var instructor_parts = line.instructor_str.Split('/');
                    var code = instructor_parts[1];
                    instructor = entities.contacts.Where(i => i.codeAIMS == code).Select(i => i.id).FirstOrDefault();
                    if (instructor == null)
                    {
                        string companyId = "";
                        if (code.StartsWith("10"))
                        {
                            companyId = entities.company.Where(c => c.codeAIMS == "S7").Select(c => c.id).FirstOrDefault();
                        }
                        else if (code.StartsWith("11"))
                        {
                            companyId = entities.company.Where(c => c.codeAIMS == "GH").Select(c => c.id).FirstOrDefault();
                        }
                        var fullname = instructor_parts[0].Split(',');
                        var instructorData = new Dictionary<string, object>(){
                            {"name", fullname[1]},
                            {"name_eng", fullname[1]},
                            {"surname", fullname[0]},
                            {"surname_eng", fullname[0]},
                            {"contact_position", "Instructor"},
                            {"contact_company", companyId},
                            {"codeAIMS", code}
                        };
                        instructor = Api.TeamNetwork.ObjectService.CreateWithAlias("contacts", instructorData);
                    }
                }

                //Определение пилота 1
                string pilot1 = null;
                if (!String.IsNullOrEmpty(line.pilot1_str))
                {
                    var pilot1_parts = line.pilot1_str.Split('/');
                    var code = pilot1_parts[1];
                    pilot1 = entities.contacts.Where(i => i.codeAIMS == code).Select(i => i.id).FirstOrDefault();
                    if (pilot1 == null)
                    {
                        string companyId = "";
                        if (code.StartsWith("10"))
                        {
                            companyId = entities.company.Where(c => c.codeAIMS == "S7").Select(c => c.id).FirstOrDefault();
                        }
                        else if (code.StartsWith("11"))
                        {
                            companyId = entities.company.Where(c => c.codeAIMS == "GH").Select(c => c.id).FirstOrDefault();
                        }
                        var fullname = pilot1_parts[0].Split(',');
                        var pilot1Data = new Dictionary<string, object>(){
                            {"name", fullname[1]},
                            {"name_eng", fullname[1]},
                            {"surname", fullname[0]},
                            {"surname_eng", fullname[0]},
                            {"contact_position", "Pilot"},
                            {"contact_company", companyId},
                            {"codeAIMS", code}
                        };
                        pilot1 = Api.TeamNetwork.ObjectService.CreateWithAlias("contacts", pilot1Data);
                    }
                }

                //Определение пилота 2
                string pilot2 = null;
                if (!String.IsNullOrEmpty(line.pilot2_str))
                {
                    var pilot2_parts = line.pilot2_str.Split('/');
                    var code = pilot2_parts[1];
                    pilot2 = entities.contacts.Where(i => i.codeAIMS == code).Select(i => i.id).FirstOrDefault();
                    if (pilot2 == null)
                    {
                        string companyId = "";
                        if (code.StartsWith("10"))
                        {
                            companyId = entities.company.Where(c => c.codeAIMS == "S7").Select(c => c.id).FirstOrDefault();
                        }
                        else if (code.StartsWith("11"))
                        {
                            companyId = entities.company.Where(c => c.codeAIMS == "GH").Select(c => c.id).FirstOrDefault();
                        }
                        var fullname = pilot2_parts[0].Split(',');
                        var pilot2Data = new Dictionary<string, object>(){
                            {"name", fullname[1]},
                            {"name_eng", fullname[1]},
                            {"surname", fullname[0]},
                            {"surname_eng", fullname[0]},
                            {"contact_position", "Pilot"},
                            {"contact_company", companyId},
                            {"codeAIMS", code}
                        };
                        pilot2 = Api.TeamNetwork.ObjectService.CreateWithAlias("contacts", pilot2Data);
                    }
                }
                //Определение Наблюдателя
                string observer = null;
                if (!String.IsNullOrEmpty(line.observer_str))
                {
                    var observer_parts = line.observer_str.Split('/');
                    var code = observer_parts[1];
                    observer = entities.contacts.Where(i => i.codeAIMS == code).Select(i => i.id).FirstOrDefault();
                    if (observer == null)
                    {
                        string companyId = "";
                        if (code.StartsWith("10"))
                        {
                            companyId = entities.company.Where(c => c.codeAIMS == "S7").Select(c => c.id).FirstOrDefault();
                        }
                        else if (code.StartsWith("11"))
                        {
                            companyId = entities.company.Where(c => c.codeAIMS == "GH").Select(c => c.id).FirstOrDefault();
                        }
                        var fullname = observer_parts[0].Split(',');
                        var observerData = new Dictionary<string, object>(){
                            {"name", fullname[1]},
                            {"name_eng", fullname[1]},
                            {"surname", fullname[0]},
                            {"surname_eng", fullname[0]},
                            {"contact_position", "Instructor"},
                            {"contact_company", companyId},
                            {"codeAIMS", code}
                        };
                        observer = Api.TeamNetwork.ObjectService.CreateWithAlias("contacts", observerData);
                    }
                }

                //Подготовка блока данных для создания сессии
                var data = new Dictionary<string, object>()
                {
                    {"session_start_date", line.session_start_date},
                    {"session_finish_date", line.session_finish_date},
                    {"session_training_apparatus", training_apparatus},
                    {"ticket", session_ticket},
                    {"fbs", fbs},
                    {"typeAIMS", line.typeAIMS_str},
                    {"leaseWET", leaseWET},
                    {"session_statys", sessionStatus},
                    {"instructor", instructor},
                    {"first_pilot", pilot1},
                    {"second_pilot", pilot2},
                    {"codeAIMS", line.session_codeAIMS_str},
                    {"observer", observer}
                };

                //Компания для текущей строки
                var currentCompanyForRow = entities.company.Where(c => c.codeAIMS == line.session_company_str && c._isDisabled != true)
                    .Select(c => c.id).Single();

                //сессии у которых совпадают даты и компания
                var partiallySameSessions = allSessions.Where(s => s.session_training_apparatus == training_apparatus && s.session_start_date.Value.Subtract(DateTime.UtcNow).TotalDays <= 7 &&
                                                                    s.session_start_date.Value.Subtract(DateTime.UtcNow).TotalDays > 0 &&
                                                                    ((ticketDictionary[s.ticket] == currentCompanyForRow) &&
                                                                    (line.session_start_date.ToUniversalTime() == s.session_start_date.Value) && (line.session_finish_date.ToUniversalTime()
                                                                    == s.session_finish_date.Value)));

                if (partiallySameSessions.Any())
                {

                    //проверка других полей
                    foreach (var session in partiallySameSessions)
                    {
                        if (session.typeAIMS != line.typeAIMS_str || session.fbs != fbs ||
                            session.instructor != instructor || session.first_pilot != pilot1 ||
                            session.second_pilot != pilot2
                            || session.codeAIMS != line.session_codeAIMS_str)
                        {
                            Api.TeamNetwork.ObjectService.EditWithAlias("sessions", session.id,
                                new Dictionary<string, object>
                                {

                                    {"typeAIMS", line.typeAIMS_str},
                                    {"fbs", fbs},
                                    {"instructor", instructor},
                                    {"first_pilot", pilot1},
                                    {"second_pilot", pilot2},
                                    {"codeAIMS", line.session_codeAIMS_str}

                                });
                        }
                    }
                }
                else
                {
                    //Выбор сессий, с которыми происходит пересечение
                    var intersectedSessionIds =
                        allSessions.Where(
                            s => s.session_training_apparatus == training_apparatus &&
                                !((ticketDictionary[s.ticket] == currentCompanyForRow) && (line.session_start_date.ToUniversalTime() == s.session_start_date.Value) && (line.session_finish_date.ToUniversalTime() == s.session_finish_date.Value)) &&
                                ((line.session_start_date.ToUniversalTime() < s.session_finish_date.Value) && (s.session_start_date.Value < line.session_finish_date.ToUniversalTime()))).Select(s => s.id);

                    //Сокрытие сессий			
                    foreach (var oldSessionId in intersectedSessionIds)
                    {
                        Api.TeamNetwork.ObjectService.Disable(oldSessionId);
                    }

                    //Проверка необходимости создания сессии
                    var mayBeSession = allSessions.Where(s => (ticketDictionary[s.ticket] == currentCompanyForRow) && s.session_training_apparatus == training_apparatus && s.session_start_date.Value == line.session_start_date.ToUniversalTime() && s.session_finish_date.Value == line.session_finish_date.ToUniversalTime()).FirstOrDefault();

                    if (mayBeSession == null)
                    {
                        Api.TeamNetwork.ObjectService.CreateWithAlias("sessions", data);
                    
                    }
                }
            }
        }
    }

    public class Session
    {
        public DateTime session_start_date { get; set; }
        public DateTime session_finish_date { get; set; }
        public string session_training_apparatus_str { get; set; }
        public string typeAIMS_str { get; set; }
        public string fbs_str { get; set; }
        public string leaseWet_str { get; set; }
        public string session_company_str { get; set; }
        public string instructor_str { get; set; }
        public string pilot1_str { get; set; }
        public string pilot2_str { get; set; }
        public string code_str { get; set; }
        public string session_codeAIMS_str { get; set; }
        public string observer_str { get; set; }
    }

}
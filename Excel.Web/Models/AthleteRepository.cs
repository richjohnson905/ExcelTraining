﻿using Excel.Entities;
using Excel.Web.DataContexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.Identity;
using System.Web.Mvc;
using System.Security.Principal;
using System.Data.Entity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace Excel.Web.Models
{
    public class AthleteRepository : IAthleteRepository
    {
        private IdentityDb db;

        public AthleteRepository()
        {
            db = new IdentityDb();
        }

        public AthleteRepository(DbContext db)
        {
            this.db = db as IdentityDb;
        }

        //Athlete
        public void GiveAdmin(Athlete athlete)
        {
            db.Entry(athlete).State = EntityState.Modified;
            var store = new UserStore<ApplicationUser>(db);
            var manager = new UserManager<ApplicationUser>(store);

            var usr = from u in db.Users
                      where u.Athlete.Id == athlete.Id
                      select u;
            manager.AddToRole(usr.First().Id, "admin");
            db.SaveChanges();
        }

        public void RemoveAdmin(Athlete athlete)
        {
            db.Entry(athlete).State = EntityState.Modified;
            var store = new UserStore<ApplicationUser>(db);
            var manager = new UserManager<ApplicationUser>(store);

            var usr = from u in db.Users
                      where u.Athlete.Id == athlete.Id
                      select u;
            manager.RemoveFromRole(usr.First().Id, "admin");
            db.SaveChanges();
        }

        public void CreateNewAthlete(Athlete athleteToCreate)
        {
            db.Athletes.Add(athleteToCreate);
            db.SaveChanges();
        }

        public void DeleteAthlete(int id)
        {
            var athleteToDelete = GetAthleteById(id);
            db.Entry(athleteToDelete).State = EntityState.Modified;
            var store = new UserStore<ApplicationUser>(db);
            var manager = new UserManager<ApplicationUser>(store);
            
            var usr = from u in db.Users
                      where u.Athlete.Id == id
                      select u;
            manager.RemoveFromRole(usr.First().Id, "admin");
            manager.Delete(usr.FirstOrDefault());
            db.Athletes.Remove(athleteToDelete);
            db.SaveChanges();
        }

        public Athlete GetAthleteById(int id)
        {
            return db.Athletes.FirstOrDefault(d => d.Id == id);
        }

        public Athlete GetAthleteByUserId(string userId)
        {
            var appUser = db.Users.Where(u => u.Id == userId).SingleOrDefault();
            return db.Athletes.FirstOrDefault(d => d.Id == appUser.Athlete.Id);
        }

        public Athlete GetAthleteByEmail(string email)
        {
            var appUser = db.Users.Where(u => u.Email == email).SingleOrDefault();
            if (appUser != null)
            {
                return db.Athletes.FirstOrDefault(d => d.Id == appUser.Athlete.Id);
            }
            return null;
        }

        public IEnumerable<Athlete> GetAllAthletes()
        {
            return db.Athletes.Where(a => a.UserType == UserTypes.Athlete);
        }

        public IEnumerable<Athlete> GetAllTrainers()
        {
            return db.Athletes.Where(t => t.UserType == UserTypes.Trainer);
        }

        public void RemoveAthleteFromSession(int sessionId, int athleteId)
        {
            var session = GetSessionById(sessionId);
            var x = session.SessionAthletes.SingleOrDefault(a => a.AthleteId == athleteId);

            session.SessionAthletes.Remove(x);
        }

        public void AddAthleteToSession(int sessionId, int athleteId)
        {
            var session = GetSessionById(sessionId);
            var athlete = GetAthleteById(athleteId);
            var sessionAthlete = new SessionAthlete {Athlete = athlete, Session = session, Confirmed = false};
            db.SessionAthletes.Add(sessionAthlete);
            db.SaveChanges();
        }

        // Sessions
        public Session GetSession(int hour, DateTime dt, int locationId, AthleteTypes athleteType)
        {
            Session session = db.Sessions.SingleOrDefault(s => s.Hour == hour &&
                s.Day.Year == dt.Year &&
                s.Day.Month == dt.Month &&
                s.Day.Day == dt.Day &&
                s.LocationId == locationId &&
                s.AthleteType == athleteType);

            return session;
        }

        public void Write_CreateSessions(DateTime dt, int locationId)
        {
            List<Schedule> personalTrainingSchedule = db.Schedules.Where(s => s.AthleteType == AthleteTypes.PersonalTraining).ToList();
            foreach (var schedule in personalTrainingSchedule)
            {
                if (schedule.IsAvailable)
                {
                    var session = new Session { Day = dt, Hour = schedule.Hour, LocationId = locationId, AthleteType = AthleteTypes.PersonalTraining };
                    if (!DoesExist(session, AthleteTypes.PersonalTraining))
                    {
                        db.Sessions.Add(new Session { Day = dt, Hour = schedule.Hour, LocationId = locationId, AthleteType = AthleteTypes.PersonalTraining });
                    }
                }
            }
            List<Schedule> sportsTrainingSchedule = db.Schedules.Where(s => s.AthleteType == AthleteTypes.SportsTraining).ToList();
            foreach (var schedule in sportsTrainingSchedule)
            {
                if (schedule.IsAvailable)
                {
                    var session = new Session { Day = dt, Hour = schedule.Hour, LocationId = locationId, AthleteType = AthleteTypes.SportsTraining };
                    if (!DoesExist(session, AthleteTypes.SportsTraining))
                    {
                        db.Sessions.Add(session);
                    }
                }
            }

            db.SaveChanges();
        }

        private bool DoesExist(Session session, AthleteTypes athleteType)
        {
            return GetSession(session.Hour, session.Day, session.LocationId, athleteType) != null;
        }

        public Session GetSessionById(int id)
        {
            return db.Sessions.FirstOrDefault(s => s.Id == id);
        }

        public IEnumerable<Session> GetFutureSessions(int athleteId)
        {
            DateTime saveNow = DateTime.Now.Date;
//            return db.Sessions.Where(s => s.Athletes.Any(a => a.Id == athleteId) && s.Day.Year >= saveNow.Year &&
//                s.Day.Month >= saveNow.Month && s.Day.Day >= saveNow.Day);

            return db.SessionAthletes
                .Where(sa => sa.AthleteId == athleteId)
                .Select(sa => sa.Session).Where(s => s.Day.Year >= saveNow.Year &&
                    s.Day.Month >= saveNow.Month && s.Day.Day >= saveNow.Day);
        }

        public IEnumerable<Session> GetPastSessions(int athleteId)
        {
            DateTime saveNow = DateTime.Now.Date;
            return db.SessionAthletes
                .Where(sa => sa.AthleteId == athleteId)
                .Select(sa => sa.Session).Where(s => s.Day.Year <= saveNow.Year &&
                        s.Day.Month <= saveNow.Month &&
                        s.Day.Day < saveNow.Day);

        }

        public IEnumerable<Athlete> GetPersonalTrainingAthletes(int sessionId)
        {
            return db.SessionAthletes.Where(sa => sa.SessionId == sessionId && sa.Confirmed == false)
                .Select(sa => sa.Athlete)
                .Where(a => a.AthleteType == AthleteTypes.PersonalTraining && a.UserType == UserTypes.Athlete);
        }

        public IEnumerable<Athlete> GetSportsTrainingAthletes(int sessionId)
        {
            return db.SessionAthletes.Where(sa => sa.SessionId == sessionId && sa.Confirmed == false)
                .Select(sa => sa.Athlete)
                .Where(a => a.AthleteType == AthleteTypes.SportsTraining && a.UserType == UserTypes.Athlete);
        }

        public Athlete GetSesssionTrainer(int sessionId)
        {
            return db.SessionAthletes
                    .Where(sa => sa.SessionId == sessionId)
                    .Select(sa => sa.Athlete).FirstOrDefault(a => a.UserType == UserTypes.Trainer);
        }

        //Locations
        public IEnumerable<Location> GetLocations()
        {
            return db.Locations;
        }

        //UserTypes
        public IEnumerable<AthleteTypes> GetAthleteTypes()
        {
            List<AthleteTypes> all = new List<AthleteTypes>() {AthleteTypes.PersonalTraining, AthleteTypes.SportsTraining};
            return all.AsEnumerable();
        }

        // Schedules
        public IEnumerable<Schedule> GetDardenneSchedule(AthleteTypes athleteType)
        {
            return db.Schedules.Where(s => s.Location.Name.Contains("Dardenne") && s.AthleteType == athleteType);
        }

        public void SetScheduleStatus(int scheduleId, bool status)
        {
            Schedule schedule = db.Schedules.Where(s => s.Id == scheduleId).FirstOrDefault();
            schedule.IsAvailable = status;
            schedule.Location = GetLocations().Where(s => s.Name.Contains("Dardenne")).FirstOrDefault();
            DoSaveChanges();
        }

        public IdentityDb GetIdentityDb()
        {
            return db;
        }

        public int SaveChanges()
        {
            return DoSaveChanges();
        }

        private int DoSaveChanges()
        {
            try
            {
                return db.SaveChanges();
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException dbEx)
            {
                Exception raise = dbEx;
                foreach (var validationErrors in dbEx.EntityValidationErrors)
                {
                    foreach (var validationError in validationErrors.ValidationErrors)
                    {
                        string message = string.Format("{0}:{1}",
                            validationErrors.Entry.Entity.ToString(),
                            validationError.ErrorMessage);
                        // raise a new exception nesting
                        // the current instance as InnerException
                        raise = new InvalidOperationException(message, raise);
                    }
                }
                throw raise;
            }
        }

        public void Dispose()
        {
            db.Dispose();
        }
    }
}
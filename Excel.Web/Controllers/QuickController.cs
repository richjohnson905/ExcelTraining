﻿using Excel.Entities;
using Excel.Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Excel.Web.Controllers
{
    public class QuickController : Controller
    {
        private IAthleteRepository athleteRepository;
        private QuickHelper helper = new QuickHelper();
      
        public QuickController()
        {
            athleteRepository = new AthleteRepository();
            //GetUserId = () => User.Identity.GetUserId();
        }

        public QuickController(IAthleteRepository db)
        {
            athleteRepository = db;
        }

        // GET: Quick
        public ActionResult Index()
        {
            QuickScheduleViewModel quickScheduleViewModel = new QuickScheduleViewModel();

            DateTime nextSession = helper.GetNextSession();
            quickScheduleViewModel.SessionDate = nextSession.ToLongDateString();
            quickScheduleViewModel.SessionTime = helper.GetSessionTimeString(nextSession);
            int locationId = helper.GetDardenne(athleteRepository).Id;

            // TODO rework to handle personal and sports
            Session session = helper.GetOrCreateSession(nextSession.Hour, nextSession, locationId, AthleteTypes.PersonalTraining, athleteRepository);
            quickScheduleViewModel.QuickAthletes = athleteRepository.GetPersonalTrainingAthletes(session.Id).ToList();
            quickScheduleViewModel.QuickAthletes = quickScheduleViewModel.QuickAthletes.Concat(athleteRepository.GetSportsTrainingAthletes(session.Id)).ToList();
            return View(quickScheduleViewModel);
        }

        public ActionResult Signup(string email)
        {
            var saveNow = helper.GetNextSession();
            var athlete = athleteRepository.GetAthleteByEmail(email);
            var session = helper.GetOrCreateSession(saveNow.Hour, saveNow, helper.GetDardenne(athleteRepository).Id, athlete.AthleteType, athleteRepository);
            if (athlete != null)
            {
                athleteRepository.AddAthleteToSession(session.Id, athlete.Id);
            }
            return RedirectToAction("Index"); 
        }        
    }
}
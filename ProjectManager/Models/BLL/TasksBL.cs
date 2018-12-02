﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ProjectManager.Models
{
    public static class TasksBL
    {
        public enum Task_Status
        {
            Discussing = 1,
            InProgress,
            WaitForConfirmed,
            Completed,
            Ended
        }
        /// <summary>
        /// 回傳帶有orderID、parentOrderID排序完成的List&lt;Task>
        /// </summary>
        /// <param name="tasks"></param>
        /// <returns></returns>
        public static IEnumerable<Tasks> GetSortedTasks(this IEnumerable<Tasks> tasks)
        {
            TreeGridModel treeGrid = new TreeGridModel(tasks.ToList());
            return treeGrid.SortedTasks;
        }
        
        public static IEnumerable<Tasks> GetLeafTasks(this IEnumerable<Tasks> tasks)
        {
            var parentTasks = tasks.Where(t=>t.ParentTaskGUID != null)
                .Select(t => t.ParentTaskGUID).Distinct().ToList();

            List<Tasks> leafTasks = new List<Tasks>();
            foreach (var item in tasks.Where(t => t.ParentTaskGUID != null))
            {
                if(!parentTasks.Where(parent=> parent == item.ProjectGUID).Any())
                {
                    leafTasks.Add(item);
                }
            }
            return leafTasks;
        }
        public static IEnumerable<Tasks> GetRootTasks(this IEnumerable<Tasks> tasks)
        {
            return tasks.Where(t => t.ParentTaskGUID == null).ToList();
        }
        public static IEnumerable<int> GetRootTasksCompletedRate(this IEnumerable<Tasks> rootTasks)
        {
            List<int> rootTasksCompletedRate = new List<int>();
            TreeGridModel treeGrid = new TreeGridModel();
            foreach (var root in rootTasks)
            {
                treeGrid.GetChildLeafTasks(root);
                int completedLeafTaskCount = 0;
                int totalLeafCount = treeGrid.ChildLeafTasks.Count();
                foreach (var leaf in treeGrid.ChildLeafTasks)
                {                    
                    string status = (leaf.TaskStatusID ?? default(int)).ToString();
                    Task_Status leaf_Status = (Task_Status)Enum.Parse(typeof(Task_Status), status);
                    if (leaf_Status == Task_Status.Completed)
                    {
                        completedLeafTaskCount++;
                    }
                }
                rootTasksCompletedRate.Add(completedLeafTaskCount / totalLeafCount * 100);
            }

            return rootTasksCompletedRate;
        } 
        public static IEnumerable<Tasks> GetAllChildTasks(this Tasks task)
        {            
            TreeGridModel model = new TreeGridModel();
            model.GetChildren(task, new Repository<Tasks>().GetCollections().ToList());
            return model.ChildTasks;
        }
        public static bool IsAnyResource(this IEnumerable<Tasks> tasks)
        {
            Repository<TaskResource> resourceRepo = new Repository<TaskResource>();
            foreach (var task in tasks)
            {
                if (resourceRepo.GetCollections().Where(r => r.TaskGUID == task.TaskGUID).Any())
                    return true;
            }
            return false;
        }
        public static IEnumerable<Group<string, DisplayWorkloadVM>>GetTeamWorkLoad (this IEnumerable<Tasks> tasks)
        {
            Repository<Tasks> resourceRepo = new Repository<Tasks>();
            var workload = resourceRepo.GetCollections().Where(t => t.EmployeeGUID != null).GroupBy(g => g.Employee.EmployeeName)
                                           .Select(g => new Group<string, DisplayWorkloadVM> { Key = g.Key, Sum = g.Sum(e => e.EstWorkTime) });
            return workload;
        }
    }
}
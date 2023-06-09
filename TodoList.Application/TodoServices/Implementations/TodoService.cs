﻿using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using TodoList.Application.TodoServices.Interfaces;
using TodoList.Domain.Common.Dtos;
using TodoList.Domain.Common.Enums;
using TodoList.Domain.Common.Interfaces;
using TodoList.Domain.TodoAggregate.Dtos;
using TodoList.Domain.TodoAggregate.Entities;
using TodoList.Domain.UserAggregate.Entities;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace TodoList.Application.TodoServices.Implementations;

public class TodoService : ITodoService
{
	private readonly IGenericRepository<Todo> _todoRepository;
	private readonly IGenericRepository<User> _userRepository;

	public TodoService(IGenericRepository<Todo> todoRepository, IGenericRepository<User> userRepository)
	{
		_todoRepository = todoRepository;
		_userRepository = userRepository;
	}

	public async Task<ResultDTO<GetTodoResult>> CreateTask(CreateTaskDTO input, long userId)
	{
		if (userId == 0) return new ResultDTO<GetTodoResult> { resultStatus = ResultStatus.NotForUser, Message = "forbidden action" };

		var newTask = new Todo
		{
			Title = input.Title,
			Description = input.Description,
			UserId = userId
		};

		await _todoRepository.AddEntity(newTask);
		await _todoRepository.SaveChanges();

		return new ResultDTO<GetTodoResult> { Message = "task added successfully" };
	}

	public async Task<ResultDTO<List<GetTodoResult>>> GetAllTasks()
	{
		var tasks = await _todoRepository.GetQuery().AsNoTracking()
		.Where(x => x.IsDeleted == false)
		.Select(x => new GetTodoResult { Id = x.Id, Title = x.Title, Description = x.Description, UserId = x.UserId, IsCompleted = x.IsCompleted })
		.ToListAsync();

		if (!tasks.Any()) return new ResultDTO<List<GetTodoResult>> { Message = "there is no task yet" };

		return new ResultDTO<List<GetTodoResult>> { Data = tasks };
	}

	public async Task<ResultDTO<List<GetTodoResult>>> GetTasksByUserId(long userId)
	{
		var task = await _todoRepository.GetQuery()
		.Where(x => x.UserId == userId && x.IsDeleted == false)
		.Select(x => new GetTodoResult
		{ Id = x.Id, Title = x.Title, Description = x.Description, UserId = x.UserId, IsCompleted = x.IsCompleted })
		.ToListAsync();

		if (!task.Any()) return new ResultDTO<List<GetTodoResult>> { Message = "there is no task for this user" };

		return new ResultDTO<List<GetTodoResult>> { Data = task };
	}

	public async Task<ResultDTO<List<GetTodoResult>>> GetTasksByUserName(string userName)
	{
		var task = await _todoRepository.GetQuery().AsNoTracking()
		.Where(x => x.IsDeleted == false && EF.Functions.Like(x.User.FullName, $"%{userName}%"))
		.Select(x => new GetTodoResult { Id = x.Id, Title = x.Title, Description = x.Description, UserId = x.UserId, IsCompleted = x.IsCompleted })
		.ToListAsync();

		if (!task.Any()) return new ResultDTO<List<GetTodoResult>> { Message = "there is no task" };

		return new ResultDTO<List<GetTodoResult>> { Data = task };
	}

	public async Task<ResultDTO<Todo>> GetTaskById(long id)
	{
		var task = await _todoRepository.GetEntityById(id);

		if (task == null) return new ResultDTO<Todo> { Message = "there is no task with this id" };

		return new ResultDTO<Todo> { Data = task };
	}

	public async Task<ResultDTO<Todo>> UpdateTask(UpdateTaskDTO input)
	{
		var taskResult = await GetTaskById(input.Id);

		if (taskResult.Data == null) return new ResultDTO<Todo> { resultStatus = ResultStatus.NotFound, Message = "there is no task with this id" };

		var task = taskResult.Data;

		task.IsDeleted = input.IsDeleted;
		task.IsCompleted = input.IsCompleted;
		task.Description = input.Description;
		task.Title = input.Title;

		_todoRepository.EditEntity(task);
		await _todoRepository.SaveChanges();

		return new ResultDTO<Todo> { resultStatus = ResultStatus.Succeded, Message = "task updated successfully" };
	}

	public async Task<ResultDTO<Todo>> DeleteTask(long todoId)
	{
		await _todoRepository.DeleteEntity(todoId);
		await _todoRepository.SaveChanges();
		return new ResultDTO<Todo> { resultStatus = ResultStatus.Succeded, Message = "task deleted successfully" };
	}

	public async ValueTask DisposeAsync()
	{
		await _todoRepository.DisposeAsync();
		await _userRepository.DisposeAsync();
	}
}

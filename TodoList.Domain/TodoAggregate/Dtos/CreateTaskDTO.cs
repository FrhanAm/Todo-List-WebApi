﻿namespace TodoList.Domain.TodoAggregate.Dtos;

public class CreateTaskDTO
{
    public string Title { get; set; }
    public string? Description { get; set; }
}

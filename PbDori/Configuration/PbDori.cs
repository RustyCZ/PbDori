﻿using PbDori.Services;

namespace PbDori.Configuration;

public class PbDori
{
    public ApiBasicAuth ApiBasicAuth { get; set; } = new ApiBasicAuth();
    public Strategy[] Strategies { get; set; } = [];
    public BackTestResultsStorage BackTestResultsStorage { get; set; } = new BackTestResultsStorage();
    public PbFileSystem PbFileSystem { get; set; } = new PbFileSystem();
}
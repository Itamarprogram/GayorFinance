﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using GayorFinance;
using model;

namespace GayorFinance.Services
{
    public class ApiClient
    {
        private readonly HttpClient _httpClient;

        // Constructor to initialize the HttpClient
        public ApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // Method to get the stock quote for a given symbol
        public async Task<StockQuote> GetStockQuote(string symbol)
        {
            try
            {
                // The call to our API that will return the stock quote based on our specified symbol
                HttpResponseMessage response = await _httpClient.GetAsync($"/api/Finance/GetStockQuote/stock/quote/{symbol}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<StockQuote>();
            }
            catch (HttpRequestException ex)
            {
                // Handle communication errors (e.g., server not found, timeout)
                throw new HttpRequestException($"Error connecting to server: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                // Handle other errors
                throw new Exception($"An unknown error occurred with message {ex.Message}", ex);
            }
        }

        // Method to get the historical stock data for a given symbol
        public async Task<HistoricalDataResponse> GetStockQuoteHistoricalData(string symbol)
        {
            try
            {
                // The call to our API that will return the historical stock data based on our specified symbol
                HttpResponseMessage response = await _httpClient.GetAsync($"/api/Finance/GetStockQuoteHistorical/stock/quote/HistoricalData/{symbol}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<HistoricalDataResponse>();
            }
            catch (HttpRequestException ex)
            {
                // Handle communication errors (e.g., server not found, timeout)
                throw new HttpRequestException($"Error connecting to server: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                // Handle other errors
                throw new Exception($"An unknown error occurred with message {ex.Message}", ex);
            }
        }

        // Method to get today's intraday stock data for a given symbol
        public async Task<List<HistoricalIntraDayQuote>> GetStockQouteTodayData(string symbol)
        {
            try
            {
                // The call to our API that will return the historical stock data based on our specified symbol
                HttpResponseMessage response = await _httpClient.GetAsync($"/api/Finance/GetStockQuoteToday/stock/quote/Today/{symbol}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<HistoricalIntraDayQuote>>();
            }
            catch (HttpRequestException ex)
            {
                // Handle communication errors (e.g., server not found, timeout)
                throw new HttpRequestException($"Error connecting to server: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                // Handle other errors
                throw new Exception($"An unknown error occurred with message {ex.Message}", ex);
            }
        }

        // Method to get the last five days' intraday stock data for a given symbol
        public async Task<List<HistoricalIntraDayQuote>> GetStockQouteFiveDaysData(string symbol)
        {
            try
            {
                // The call to our API that will return the historical stock data based on our specified symbol
                HttpResponseMessage response = await _httpClient.GetAsync($"/api/Finance/GetStockQuoteFiveDays/stock/quote/FiveDays/{symbol}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<HistoricalIntraDayQuote>>();
            }
            catch (HttpRequestException ex)
            {
                // Handle communication errors (e.g., server not found, timeout)
                throw new HttpRequestException($"Error connecting to server: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                // Handle other errors
                throw new Exception($"An unknown error occurred with message {ex.Message}", ex);
            }
        }

        // Method to get the historical stock data for a given symbol within a date range
        public async Task<HistoricalDataResponse> GetStockQuoteHistoricalDataFromADate(string symbol, string StartDate, string EndDate)
        {
            try
            {
                // The call to our API that will return the historical stock data based on our specified symbol
                HttpResponseMessage response = await _httpClient.GetAsync($"/api/Finance/GetStockQuoteHistoricalFromADate/stock/quote/ADate/{symbol}/{StartDate}/{EndDate}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<HistoricalDataResponse>();
            }
            catch (HttpRequestException ex)
            {
                // Handle communication errors (e.g., server not found, timeout)
                throw new HttpRequestException($"Error connecting to server: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                // Handle other errors
                throw new Exception($"An unknown error occurred with message {ex.Message}", ex);
            }
        }
    }
}
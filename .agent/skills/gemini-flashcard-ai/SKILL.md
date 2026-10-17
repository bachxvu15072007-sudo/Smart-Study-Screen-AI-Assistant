---
name: gemini-flashcard-ai
description: >-
  Guides the implementation of GeminiApiService using async HttpClient for the 4 AI ComboBox modes
  (Translate, Summarize, Explain, Quiz) and automatic JSON Flashcard generation.
  Use this skill when working on Member 2 tasks (GeminiApiService, FlashcardViewModel, Flip Card review mode).
---

# Skill: Tích Hợp Google Gemini REST API & Tự Động Sinh Flashcard (`gemini-flashcard-ai`)

Sử dụng skill này khi cài đặt hoặc gỡ lỗi dịch vụ AI và màn hình ôn tập thẻ nhớ Flashcard của **Thành viên 2**.

## 1. Xử lý 4 Chế độ Cố định từ `ComboBox`
Trên `StudyNotesView`, người dùng chọn 1 trong 4 chế độ từ `ComboBox`:
1. `"Dịch sang Tiếng Việt"` $\rightarrow$ Prompt: *"Hãy dịch chính xác đoạn văn bản sau sang tiếng Việt tự nhiên, dễ hiểu:\n\n"*
2. `"Tóm tắt ý chính"` $\rightarrow$ Prompt: *"Hãy tóm tắt ngắn gọn các ý chính quan trọng nhất của đoạn nội dung sau bằng tiếng Việt (dùng gạch đầu dòng):\n\n"*
3. `"Giải thích thuật ngữ / Code"` $\rightarrow$ Prompt: *"Hãy giải thích chi tiết các thuật ngữ chuyên ngành hoặc ý nghĩa đoạn code sau cho sinh viên CNTT dễ hiểu:\n\n"*
4. `"Tạo câu hỏi ôn tập"` $\rightarrow$ Prompt: *"Dựa vào nội dung sau, hãy đặt 3 câu hỏi ôn tập kèm đáp án ngắn gọn:\n\n"*

## 2. Tự động Sinh Danh sách `Flashcard` bằng JSON
Khi người dùng bấm **"✨ AI Tự tạo Flashcard"**, gửi yêu cầu với hướng dẫn trả về JSON thuần:
```text
Dựa vào nội dung bài học sau, hãy tạo từ 3 đến 5 thẻ ghi nhớ (Flashcard) bằng tiếng Việt.
Chỉ trả về đúng một mảng JSON theo cấu trúc:
[{"question": "Câu hỏi ngắn gọn?", "answer": "Đáp án súc tích"}]
Không thêm markdown ```json hay lời giải thích nào khác.
Nội dung:
```
Sau khi nhận phản hồi từ Gemini API:
- Dùng `.Trim()` và cắt bỏ chuỗi ````json` / ```` nếu AI vẫn bọc trong code block.
- Dùng `JsonSerializer.Deserialize<List<FlashcardDto>>(jsonText, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })` để chuyển thành các đối tượng `Flashcard` và lưu vào bảng `Flashcard` thông qua `IStudyRepository`.

## 3. Xử lý Ngoại lệ An toàn (Tiêu chí 1)
- Nếu `apiKey` trống $\rightarrow$ Ném thông báo yêu cầu người dùng nhập Gemini API Key ở góc dưới thanh điều hướng.
- Luôn bọc lời gọi `HttpClient.PostAsync` trong `try/catch (Exception ex)` để trả về thông báo lỗi thân thiện khi mất mạng hoặc sai API Key.

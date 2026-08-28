class LectureNoteDto {
  final int id;
  final int lectureId;
  final String title;
  final String content;

  LectureNoteDto({
    required this.id,
    required this.lectureId,
    required this.title,
    required this.content,
  });

  factory LectureNoteDto.fromJson(Map<String, dynamic> json) {
    return LectureNoteDto(
      id: json['id'] as int? ?? 0,
      lectureId: json['lectureId'] as int? ?? 0,
      title: json['title'] as String? ?? '',
      content: json['content'] as String? ?? '',
    );
  }

  Map<String, dynamic> toJson() => {
    'id': id,
    'lectureId': lectureId,
    'title': title,
    'content': content,
  };
}

class LectureNoteRequest {
  final String title;
  final String content;

  LectureNoteRequest({required this.title, required this.content});

  Map<String, dynamic> toJson() => {
    'title': title,
    'content': content,
  };
}


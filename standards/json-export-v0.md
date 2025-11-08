# StickyHomeworks JSON 导出格式标准 (v0)

## 概述

本文档定义了 StickyHomeworks 应用程序导出的 JSON 数据格式 v0 版本的标准。该格式用于在不同实例之间迁移数据或备份作业信息。

## 格式规范

### 根对象

导出的 JSON 文件包含以下根级字段：

| 字段名 | 类型 | 必需 | 描述 |
|--------|------|------|------|
| Version | 整数 | 是 | 格式版本号，对于此标准为 0 |
| Description | 字符串 | 是 | 导出文件的描述信息 |
| ExportDate | 字符串 (ISO 8601) | 是 | 导出时间戳 |
| Homeworks | 数组 | 是 | 作业列表 |

### Homework 对象

Homeworks 数组中的每个元素都遵循以下结构：

| 字段名 | 类型 | 必需 | 描述 |
|--------|------|------|------|
| Content | 字符串 | 是 | 作业内容 |
| Subject | 字符串 | 是 | 作业科目 |
| DueTime | 字符串 (ISO 8601) | 是 | 作业截止时间 |
| Tags | 字符串数组 | 否 | 作业标签列表 |

## 示例

```json
{
  "Version": 0,
  "Description": "StickyHomeworks数据导出文件",
  "ExportDate": "2025-11-08T09:05:57",
  "Homeworks": [
    {
      "Content": "完成第5章练习题1-20题",
      "Subject": "数学",
      "DueTime": "2025-11-15T00:00:00",
      "Tags": [
        "代数",
        "几何"
      ]
    },
    {
      "Content": "阅读理解文章并写一篇200字作文",
      "Subject": "英语",
      "DueTime": "2025-11-16T00:00:00",
      "Tags": [
        "阅读",
        "写作"
      ]
    }
  ]
}
```

## 注意事项

1. 所有时间戳都应使用 ISO 8601 格式
2. 字段名称使用 PascalCase 命名约定
3. Tags 字段可能为空数组或完全缺失
4. 此格式版本为初始版本，未来可能通过递增 Version 字段引入变更
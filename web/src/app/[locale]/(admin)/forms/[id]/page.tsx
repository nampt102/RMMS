"use client";

import {
  ArrowLeftOutlined,
  DeleteOutlined,
  EditOutlined,
  HolderOutlined,
  HistoryOutlined,
  PlusOutlined,
} from "@ant-design/icons";
import { DndContext, KeyboardSensor, PointerSensor, closestCenter, useSensor, useSensors } from "@dnd-kit/core";
import type { DragEndEvent } from "@dnd-kit/core";
import {
  SortableContext,
  arrayMove,
  sortableKeyboardCoordinates,
  useSortable,
  verticalListSortingStrategy,
} from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import {
  App,
  Button,
  Card,
  Checkbox,
  Drawer,
  Dropdown,
  Empty,
  Form,
  Input,
  List,
  Modal,
  Select,
  Space,
  Spin,
  Switch,
  Tag,
  Typography,
} from "antd";
import { useLocale, useTranslations } from "next-intl";
import { useParams, useRouter } from "next/navigation";
import { useEffect, useMemo, useState } from "react";
import { useForm, useFormVersions, usePublishForm, useUpdateForm } from "@/features/forms/api";
import {
  CHOICE_TYPES,
  FIELD_TYPES,
  type FieldDef,
  type FieldTypeKey,
  type FormRules,
  parseSchema,
} from "@/features/forms/types";
import { errorCodeFromUnknown } from "@/features/auth/lib/auth-error";

const { Text, Title } = Typography;

const RULE_FLAGS: (keyof FormRules)[] = [
  "require_check_in",
  "gps_required",
  "photo_required",
  "scored",
  "allow_edit_after_submit",
  "allow_offline_draft",
];

export default function FormBuilderPage() {
  const t = useTranslations("forms");
  const tErrors = useTranslations("errors");
  const locale = useLocale();
  const router = useRouter();
  const params = useParams();
  const id = params.id as string;
  const { message } = App.useApp();

  const { data: form, isLoading, refetch } = useForm(id);
  const updateForm = useUpdateForm();
  const publishForm = usePublishForm();

  const [meta] = Form.useForm();
  const [fields, setFields] = useState<FieldDef[]>([]);
  const [rules, setRules] = useState<FormRules>({});
  const [editing, setEditing] = useState<FieldDef | null>(null);
  const [historyOpen, setHistoryOpen] = useState(false);

  // Hydrate local builder state once the form loads.
  useEffect(() => {
    if (!form) return;
    const parsed = parseSchema(form.schema);
    setFields(parsed.fields);
    setRules(parsed.rules);
    meta.setFieldsValue({
      nameVi: form.nameVi,
      nameEn: form.nameEn,
      descriptionVi: form.descriptionVi ?? "",
      descriptionEn: form.descriptionEn ?? "",
    });
  }, [form, meta]);

  const sensors = useSensors(
    useSensor(PointerSensor, { activationConstraint: { distance: 5 } }),
    useSensor(KeyboardSensor, { coordinateGetter: sortableKeyboardCoordinates }),
  );

  const showError = (error: unknown) => {
    const code = errorCodeFromUnknown(error);
    message.error(tErrors.has(code) ? tErrors(code) : tErrors("INTERNAL_ERROR"));
  };

  const nextFieldId = useMemo(() => {
    const nums = fields.map((f) => Number(/^q(\d+)$/.exec(f.id)?.[1] ?? 0));
    return `q${Math.max(0, ...nums) + 1}`;
  }, [fields]);

  const addField = (type: FieldTypeKey) => {
    const field: FieldDef = {
      id: nextFieldId,
      type,
      label_vi: "",
      label_en: "",
      required: false,
      ...(CHOICE_TYPES.includes(type) ? { options: [] } : {}),
    };
    setEditing(field);
  };

  const onDragEnd = (e: DragEndEvent) => {
    const { active, over } = e;
    if (!over || active.id === over.id) return;
    setFields((prev) => {
      const from = prev.findIndex((f) => f.id === active.id);
      const to = prev.findIndex((f) => f.id === over.id);
      return arrayMove(prev, from, to);
    });
  };

  const upsertField = (field: FieldDef) => {
    setFields((prev) => {
      const idx = prev.findIndex((f) => f.id === field.id);
      if (idx === -1) return [...prev, field];
      const next = [...prev];
      next[idx] = field;
      return next;
    });
    setEditing(null);
  };

  const removeField = (fieldId: string) => setFields((prev) => prev.filter((f) => f.id !== fieldId));

  const buildSchema = () => JSON.stringify({ fields, rules });

  const saveDraft = async (): Promise<boolean> => {
    try {
      const m = await meta.validateFields();
      await updateForm.mutateAsync({
        id,
        payload: {
          nameVi: m.nameVi,
          nameEn: m.nameEn,
          descriptionVi: m.descriptionVi || undefined,
          descriptionEn: m.descriptionEn || undefined,
          schema: buildSchema(),
        },
      });
      message.success(t("saveSuccess"));
      refetch();
      return true;
    } catch (error) {
      if ((error as { errorFields?: unknown }).errorFields) return false; // form validation
      showError(error);
      return false;
    }
  };

  const onPublish = async () => {
    const saved = await saveDraft();
    if (!saved) return;
    try {
      const res = await publishForm.mutateAsync(id);
      message.success(t("publishSuccess", { version: res.version }));
      refetch();
    } catch (error) {
      showError(error);
    }
  };

  if (isLoading || !form) {
    return (
      <div className="flex justify-center py-20">
        <Spin />
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-3xl">
      <div className="mb-4 flex items-center justify-between gap-2">
        <Space>
          <Button icon={<ArrowLeftOutlined />} onClick={() => router.push(`/${locale}/forms`)} />
          <div>
            <Title level={4} className="!mb-0">
              {form.code}
            </Title>
            <Text type="secondary">
              {t(`type_${form.formType}`)} · v{form.currentVersion}
              {form.hasDraft && <Tag className="ml-2" color="gold">{t("draftPending")}</Tag>}
            </Text>
          </div>
        </Space>
        <Space>
          <Button icon={<HistoryOutlined />} onClick={() => setHistoryOpen(true)}>
            {t("versions")}
          </Button>
          <Button onClick={saveDraft} loading={updateForm.isPending}>
            {t("saveDraft")}
          </Button>
          <Button type="primary" onClick={onPublish} loading={publishForm.isPending}>
            {t("publish")}
          </Button>
        </Space>
      </div>

      <Card title={t("metaSection")} className="mb-4" size="small">
        <Form form={meta} layout="vertical">
          <Form.Item name="nameVi" label={t("nameVi")} rules={[{ required: true, max: 255 }]}>
            <Input />
          </Form.Item>
          <Form.Item name="nameEn" label={t("nameEn")} rules={[{ required: true, max: 255 }]}>
            <Input />
          </Form.Item>
          <Form.Item name="descriptionVi" label={t("descriptionVi")}>
            <Input.TextArea rows={2} />
          </Form.Item>
          <Form.Item name="descriptionEn" label={t("descriptionEn")}>
            <Input.TextArea rows={2} />
          </Form.Item>
        </Form>
      </Card>

      <Card
        size="small"
        className="mb-4"
        title={`${t("fieldsSection")} (${fields.length})`}
        extra={
          <Dropdown
            menu={{
              items: FIELD_TYPES.map((ft) => ({ key: ft, label: t(`field_${ft}`) })),
              onClick: ({ key }) => addField(key as FieldTypeKey),
            }}
          >
            <Button type="dashed" icon={<PlusOutlined />}>
              {t("addField")}
            </Button>
          </Dropdown>
        }
      >
        {fields.length === 0 ? (
          <Empty description={t("noFields")} />
        ) : (
          <DndContext sensors={sensors} collisionDetection={closestCenter} onDragEnd={onDragEnd}>
            <SortableContext items={fields.map((f) => f.id)} strategy={verticalListSortingStrategy}>
              <div className="flex flex-col gap-2">
                {fields.map((f) => (
                  <SortableFieldRow
                    key={f.id}
                    field={f}
                    locale={locale}
                    typeLabel={t(`field_${f.type}`)}
                    onEdit={() => setEditing(f)}
                    onDelete={() => removeField(f.id)}
                  />
                ))}
              </div>
            </SortableContext>
          </DndContext>
        )}
      </Card>

      <Card size="small" title={t("rulesSection")}>
        <div className="mb-3">
          <Text type="secondary">{t("targetUsers")}</Text>
          <Select
            mode="multiple"
            className="mt-1 w-full"
            value={rules.target_users ?? []}
            onChange={(v) => setRules((r) => ({ ...r, target_users: v }))}
            options={[
              { value: "pg", label: "PG" },
              { value: "leader", label: "Leader" },
            ]}
          />
        </div>
        <div className="flex flex-col gap-2">
          {RULE_FLAGS.map((flag) => (
            <Checkbox
              key={flag}
              checked={!!rules[flag]}
              onChange={(e) => setRules((r) => ({ ...r, [flag]: e.target.checked }))}
            >
              {t(`rule_${flag}`)}
            </Checkbox>
          ))}
        </div>
      </Card>

      {editing && (
        <FieldEditorModal
          field={editing}
          existingIds={fields.map((f) => f.id)}
          onCancel={() => setEditing(null)}
          onOk={upsertField}
        />
      )}

      <Drawer title={t("versions")} open={historyOpen} onClose={() => setHistoryOpen(false)} width={360}>
        <VersionList id={id} locale={locale} />
      </Drawer>
    </div>
  );
}

function SortableFieldRow({
  field,
  locale,
  typeLabel,
  onEdit,
  onDelete,
}: {
  field: FieldDef;
  locale: string;
  typeLabel: string;
  onEdit: () => void;
  onDelete: () => void;
}) {
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({ id: field.id });
  const label = (locale === "en" ? field.label_en : field.label_vi) || field.id;
  return (
    <div
      ref={setNodeRef}
      style={{ transform: CSS.Transform.toString(transform), transition, opacity: isDragging ? 0.5 : 1 }}
      className="flex items-center gap-2 rounded-lg border border-neutral-200 bg-white px-3 py-2"
    >
      <button
        type="button"
        {...attributes}
        {...listeners}
        aria-label="drag"
        className="cursor-grab touch-none text-neutral-400 hover:text-neutral-600"
        style={{ minWidth: 44, minHeight: 44, display: "flex", alignItems: "center", justifyContent: "center" }}
      >
        <HolderOutlined />
      </button>
      <Tag color="blue">{typeLabel}</Tag>
      <span className="flex-1 truncate">
        {label}
        {field.required && <span className="ml-1 text-red-500">*</span>}
      </span>
      <Text type="secondary" className="font-mono text-xs">
        {field.id}
      </Text>
      <Button type="text" icon={<EditOutlined />} onClick={onEdit} aria-label="edit" />
      <Button type="text" danger icon={<DeleteOutlined />} onClick={onDelete} aria-label="delete" />
    </div>
  );
}

function FieldEditorModal({
  field,
  existingIds,
  onCancel,
  onOk,
}: {
  field: FieldDef;
  existingIds: string[];
  onCancel: () => void;
  onOk: (f: FieldDef) => void;
}) {
  const t = useTranslations("forms");
  const [form] = Form.useForm();
  const isChoice = CHOICE_TYPES.includes(field.type);
  const isNew = !existingIds.includes(field.id);

  useEffect(() => {
    form.setFieldsValue({
      id: field.id,
      label_vi: field.label_vi,
      label_en: field.label_en,
      required: field.required ?? false,
      options: field.options ?? [],
    });
  }, [field, form]);

  return (
    <Modal title={t("editField")} open onCancel={onCancel} onOk={() => form.submit()} destroyOnHidden width={560}>
      <Form
        form={form}
        layout="vertical"
        onFinish={(v) =>
          onOk({
            ...field,
            id: (v.id as string).trim(),
            label_vi: v.label_vi as string,
            label_en: v.label_en as string,
            required: !!v.required,
            ...(isChoice ? { options: v.options ?? [] } : {}),
          })
        }
      >
        <Form.Item
          name="id"
          label={t("fieldId")}
          tooltip={t("fieldIdHint")}
          rules={[
            { required: true },
            { pattern: /^[a-zA-Z0-9_]+$/, message: t("fieldIdInvalid") },
            {
              validator: (_, value: string) =>
                isNew && existingIds.includes(value)
                  ? Promise.reject(new Error(t("fieldIdDup")))
                  : Promise.resolve(),
            },
          ]}
        >
          <Input disabled={!isNew} />
        </Form.Item>
        <Form.Item name="label_vi" label={t("labelVi")} rules={[{ required: true }]}>
          <Input />
        </Form.Item>
        <Form.Item name="label_en" label={t("labelEn")} rules={[{ required: true }]}>
          <Input />
        </Form.Item>
        <Form.Item name="required" label={t("required")} valuePropName="checked">
          <Switch />
        </Form.Item>

        {isChoice && (
          <>
            <Text type="secondary">{t("options")}</Text>
            <Form.List name="options">
              {(items, { add, remove }) => (
                <div className="mt-2 flex flex-col gap-2">
                  {items.map((it) => (
                    <Space key={it.key} align="baseline" className="flex">
                      <Form.Item {...it} name={[it.name, "value"]} rules={[{ required: true }]} noStyle>
                        <Input placeholder={t("optValue")} style={{ width: 120 }} />
                      </Form.Item>
                      <Form.Item {...it} name={[it.name, "label_vi"]} rules={[{ required: true }]} noStyle>
                        <Input placeholder={t("optLabelVi")} style={{ width: 150 }} />
                      </Form.Item>
                      <Form.Item {...it} name={[it.name, "label_en"]} rules={[{ required: true }]} noStyle>
                        <Input placeholder={t("optLabelEn")} style={{ width: 150 }} />
                      </Form.Item>
                      <Button type="text" danger icon={<DeleteOutlined />} onClick={() => remove(it.name)} />
                    </Space>
                  ))}
                  <Button type="dashed" onClick={() => add({ value: "", label_vi: "", label_en: "" })} icon={<PlusOutlined />}>
                    {t("addOption")}
                  </Button>
                </div>
              )}
            </Form.List>
          </>
        )}
      </Form>
    </Modal>
  );
}

function VersionList({ id, locale }: { id: string; locale: string }) {
  const t = useTranslations("forms");
  const { data: versions, isLoading } = useFormVersions(id);
  const fmt = (v: string | null) => (v ? new Date(v).toLocaleString(locale === "en" ? "en-US" : "vi-VN") : "—");
  return (
    <List
      loading={isLoading}
      dataSource={versions ?? []}
      renderItem={(v) => (
        <List.Item>
          <List.Item.Meta
            title={
              <span>
                v{v.version}{" "}
                {v.isPublished ? (
                  <Tag color="green">{t("status_published")}</Tag>
                ) : (
                  <Tag color="gold">{t("draftPending")}</Tag>
                )}
              </span>
            }
            description={v.isPublished ? t("publishedAt", { at: fmt(v.publishedAt) }) : fmt(v.createdAt)}
          />
        </List.Item>
      )}
    />
  );
}

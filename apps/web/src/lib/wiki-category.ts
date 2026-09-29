/** Client-side hints only; server owns validation and reference protection. */
export type CategoryInput = {
  name: string;
  slug: string;
  displayOrder: number;
  isActive: boolean;
};

export function validateCategory(input: CategoryInput): Record<string,string> {
  const errors: Record<string,string> = {};
  if (!input.name?.trim() || input.name.trim().length > 80) errors.name = 'Tên danh mục cần từ 1 đến 80 ký tự.';
  if (!/^[a-z0-9]+(?:-[a-z0-9]+)*$/.test(input.slug ?? '') || input.slug.length < 3 || input.slug.length > 80)
    errors.slug = 'Slug cần 3–80 ký tự, chỉ gồm a-z, 0-9 và dấu nối đơn.';
  if (!Number.isInteger(input.displayOrder) || input.displayOrder < 0 || input.displayOrder > 10000)
    errors.displayOrder = 'Thứ tự phải là số nguyên từ 0 đến 10.000.';
  if (typeof input.isActive !== 'boolean') errors.isActive = 'Trạng thái danh mục không hợp lệ.';
  return errors;
}

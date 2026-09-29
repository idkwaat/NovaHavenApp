export type TagInput={name:string;slug:string;isActive:boolean};

/** Form feedback only. Server repeats validation and protects referenced tags. */
export function validateTag(input:TagInput):Record<string,string>{
  const errors:Record<string,string>={};
  if(!input.name?.trim()||input.name.trim().length>80) errors.name='Tên tag phải có 1–80 ký tự.';
  if(!/^[a-z0-9]+(?:-[a-z0-9]+)*$/.test(input.slug??'')||input.slug.length<3||input.slug.length>80)
    errors.slug='Slug tag phải có 3–80 ký tự a-z, 0-9 hoặc dấu nối đơn.';
  if(typeof input.isActive!=='boolean') errors.isActive='Trạng thái tag không hợp lệ.';
  return errors;
}

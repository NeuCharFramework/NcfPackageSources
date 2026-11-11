// Scene registry: metadata for the picker plus a lazy loader per scene.
import { CUSTOM_COPY } from '../custom/copy.js';
// V busts stale caches of lazily loaded scene modules on hosts without no-cache headers; bump it when scenes change.
// Only the entry modules carry it: what they import (js/engine/*, and js/custom/* under mount.js) is unversioned.
const V = 'v=13';
export const SCENES = [
  { id: 'home', title: 'Home, away for a week', promise: 'Say what you want. Never name a device.', thumb: 'linear-gradient(135deg,#F6E7C9,#D9B88C)', load: () => import(`./home.js?${V}`) },
  { id: 'lab', title: 'Laser lab', promise: 'Exact instructions, executed and verified step by step.', tag: 'illustrative', thumb: 'linear-gradient(135deg,#2B2E31,#5B6770)', load: () => import(`./lab.js?${V}`) },
  { id: 'elder', title: 'Elder care', promise: 'Knowing mum got up — without a camera in her room.', thumb: 'linear-gradient(135deg,#FFF1DC,#F3CFA5)', load: () => import(`./elder.js?${V}`) },
  { id: 'warehouse', title: 'Warehouse', promise: 'Coordination that re-routes when a unit drops out.', tag: 'illustrative', thumb: 'linear-gradient(135deg,#9EA3A0,#E4B53A)', load: () => import(`./warehouse.js?${V}`) },
  { id: 'creator', title: 'Creator desk', promise: 'One command. Whole setup. Backup when it counts.', thumb: 'linear-gradient(135deg,#1B2430,#6B4FD8)', load: () => import(`./creator.js?${V}`) },
];
// The 6th card, "Build your own": not a scripted scene (no prompts, no scene file here); its page is js/custom/mount.js.
export const CUSTOM = { id: 'custom', title: CUSTOM_COPY.cardTitle, promise: CUSTOM_COPY.cardPromise, tag: CUSTOM_COPY.cardTag, thumb: 'linear-gradient(135deg,#DFF6F2,#9FD9E1)', load: () => import(`../custom/mount.js?${V}`) };
